using Klipper.Purge.Console.Moonraker;
using Klipper.Purge.Console.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace Klipper.Purge.Console.Jobs
{
    public class FilePurgeJob : IJob
    {
        private readonly ILogger<FilePurgeJob> _logger;

        private readonly IOptions<FilePurgeOptions> _options;

        private readonly IMoonrakerClient _moonrakerClient;

        private readonly DateTime _purgeBefore;

        public FilePurgeJob(ILogger<FilePurgeJob> logger, IOptions<FilePurgeOptions> options, IMoonrakerClient moonrakerClient)
        {
            _logger = logger;
            _options = options;
            _moonrakerClient = moonrakerClient;
            _purgeBefore = DateTime.Now.AddDays(_options.Value.PurgeOlderThanDays * -1);
        }

        public static void Register(FilePurgeOptions options, IServiceCollectionQuartzConfigurator quartz)
        {
            if (options.Enabled == false)
                return;

            var jobKey = new JobKey("file-purge");

            quartz.AddJob<FilePurgeJob>(jobKey, job => job.WithDescription(""));

            if (options.RunOnStartup)
                quartz.AddTrigger(trigger => trigger.ForJob(jobKey).StartNow());

            if (CronExpression.IsValidExpression(options.Schedule) == false)
                return;

            quartz.AddTrigger(trigger => trigger.ForJob(jobKey).WithCronSchedule(options.Schedule));
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Starting file purge");

            var printStatusTask = _moonrakerClient.GetPrintStatusAsync();
            var jobListTask = _moonrakerClient.ListJobsAsync();
            var fileListTask = _moonrakerClient.ListFilesAsync();
            var fileListResult = await fileListTask;

            if (fileListResult == null || fileListResult.Files.Any() == false)
                return;

            var files = fileListResult.Files;

            _logger.LogInformation($"{files.Count} files to process");

            var jobListResult = await jobListTask;
            var jobs = jobListResult?.Jobs ?? new List<Job>();

            _logger.LogInformation($"{jobs.Count} jobs currently queued");

            var printStatusResult = await printStatusTask;
            var printStatus = printStatusResult ?? throw new InvalidOperationException("Unable to obtain the current print's state");

            foreach (var file in files)
            {
                _logger.LogInformation($"Processing file {file.Path}");

                if (string.Equals("printing", printStatus.State, StringComparison.CurrentCultureIgnoreCase) && string.Equals(printStatus.Filename, file.Path, StringComparison.CurrentCultureIgnoreCase))
                {
                    _logger.LogInformation("File is currently being printed");

                    continue;
                }

                if (DateTime.UnixEpoch.AddSeconds(file.Modified) > _purgeBefore)
                {
                    _logger.LogInformation("File is too new");

                    continue;
                }

                if (jobs.Any(x => string.Equals(x.Path, file.Path, StringComparison.CurrentCultureIgnoreCase)) && _options.Value.ExcludeQueued)
                {
                    _logger.LogInformation("File is queued and queued items are excluded");

                    continue;
                }

                _logger.LogInformation("Removing file(s) from queue");

                await Task.WhenAll(jobs.Where(x => x.Path == file.Path).Select(x => _moonrakerClient.DeleteJobAsync(x.Path)));

                _logger.LogInformation("Deleting file");

                await _moonrakerClient.DeleteFileAsync(file.Path);
            }

            return;
        }
    }
}