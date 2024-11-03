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

            var printerStatusTask = _moonrakerClient.GetPrinterStatusAsync();
            var jobQueueStatusTask = _moonrakerClient.GetJobQueueStatusAsync();
            var fileListTask = _moonrakerClient.ListFilesAsync();
            var fileListResult = await fileListTask;

            if (fileListResult?.Files == null)
                throw new InvalidOperationException("Unable to retrieve file list");

            _logger.LogInformation($"{fileListResult.Files.Count} files to process");

            var jobQueueStatusResult = await jobQueueStatusTask;

            if (jobQueueStatusResult?.Result?.Jobs == null)
                throw new InvalidOperationException("Unable to retrieve current job queue");

            _logger.LogInformation($"{jobQueueStatusResult.Result.Jobs.Count} jobs currently queued");

            var printerStatusResult = await printerStatusTask;

            if (printerStatusResult?.Result?.Status?.PrintStatus == null)
                throw new InvalidOperationException("Unable to retrieve current print status");

            foreach (var file in fileListResult.Files)
            {
                _logger.LogInformation($"Processing file {file.Path}");

                if (string.Equals("printing", printerStatusResult.Result.Status.PrintStatus.State, StringComparison.CurrentCultureIgnoreCase) && string.Equals(printerStatusResult.Result.Status.PrintStatus.Filename, file.Path, StringComparison.CurrentCultureIgnoreCase))
                {
                    _logger.LogInformation("File is currently being printed");

                    continue;
                }

                if (DateTime.UnixEpoch.AddSeconds(file.Modified) > _purgeBefore)
                {
                    _logger.LogInformation("File is too new");

                    continue;
                }

                if (jobQueueStatusResult.Result.Jobs.Any(x => string.Equals(x.Path, file.Path, StringComparison.CurrentCultureIgnoreCase)) && _options.Value.ExcludeQueued)
                {
                    _logger.LogInformation("File is queued and queued items are excluded");

                    continue;
                }

                _logger.LogInformation("Removing file(s) from queue");

                await Task.WhenAll(jobQueueStatusResult.Result.Jobs.Where(x => x.Path == file.Path).Select(x => _moonrakerClient.DeleteJobAsync(x.Path)));

                _logger.LogInformation("Deleting file");

                await _moonrakerClient.DeleteFileAsync(file.Path);
            }

            return;
        }
    }
}