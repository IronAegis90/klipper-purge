using System.Text.RegularExpressions;
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

            var listJobsTask = _moonrakerClient.ListJobsAsync();
            var printStatusTask = _moonrakerClient.GetPrintStatusAsync();
            var printStatusResult = await printStatusTask;

            if (printStatusResult == null)
                throw new InvalidOperationException("Unable to retrieve current print status");

            var listJobsResult = await listJobsTask;

            if (listJobsResult?.Jobs == null)
                throw new InvalidOperationException("Unable to retrieve current job queue");

            _logger.LogInformation($"{listJobsResult.Jobs.Count} jobs currently queued");

            await ProcessDirectory("gcodes", string.Empty, printStatusResult, listJobsResult.Jobs);

            return;
        }

        private async Task ProcessDirectory(string directoryName, string parent, PrintStatus printStatus, List<Job> jobs)
        {
            _logger.LogInformation($"Processing the {directoryName} directory");

            var directoryPath = $"{parent}{directoryName}";
            var listDirectoriesResult = await _moonrakerClient.ListDirectoriesAsync(directoryPath);

            if (listDirectoriesResult?.Directories == null)
                throw new InvalidOperationException("Unable to retrieve directory metadata");

            await Task.WhenAll(listDirectoriesResult.Directories.Select(x => ProcessDirectory(x.Name, $"{directoryPath}/", printStatus, jobs)));

            _logger.LogInformation($"{listDirectoriesResult.Files.Count} files to process");

            foreach (var file in listDirectoriesResult.Files)
            {
                var fullFileName = $"{directoryPath}/{file.Name}";
                var fileName = Regex.Replace(fullFileName, $"^gcodes/", string.Empty);

                _logger.LogInformation($"Processing file {file.Name}");

                if (Path.GetExtension(fileName) != ".gcode")
                {
                    _logger.LogInformation("File is not of type GCode and will not be deleted");

                    continue;
                }

                if (string.Equals("printing", printStatus.State, StringComparison.CurrentCultureIgnoreCase) &&
                    string.Equals(printStatus.Filename, fileName, StringComparison.CurrentCultureIgnoreCase))
                {
                    _logger.LogInformation("File is currently being printed");

                    continue;
                }

                if (DateTime.UnixEpoch.AddSeconds(file.Modified) > _purgeBefore)
                {
                    _logger.LogInformation("File is too new");

                    continue;
                }

                if (jobs.Any(x => string.Equals(x.Name, fileName, StringComparison.CurrentCultureIgnoreCase)) &&
                    _options.Value.ExcludeQueued)
                {
                    _logger.LogInformation("File is queued and queued items are excluded");

                    continue;
                }

                _logger.LogInformation("Removing file(s) from queue");

                await Task.WhenAll(jobs.Where(x => x.Name == fileName).Select(x => _moonrakerClient.DeleteJobAsync(x.Id)));

                _logger.LogInformation("Deleting file");

                await _moonrakerClient.DeleteFileAsync(fileName);
            }

            var listUpdatedDirectoriesResult = await _moonrakerClient.ListDirectoriesAsync(directoryPath);

            if (listUpdatedDirectoriesResult == null)
                throw new InvalidOperationException("Unable to retrieve directory metadata");

            if (listUpdatedDirectoriesResult.Directories.Any() == false && listUpdatedDirectoriesResult.Files.Any() == false)
                await _moonrakerClient.DeleteDirectoryAsync(directoryPath);
        }
    }
}