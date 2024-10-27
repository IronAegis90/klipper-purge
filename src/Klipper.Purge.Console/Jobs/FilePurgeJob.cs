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

            _purgeBefore = DateTime.Now.AddDays(_options.Value.PurgeOlderThanDays * -1).Date;
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

            var jobListTask = _moonrakerClient.ListJobsAsync();
            var fileListTask = _moonrakerClient.ListFilesAsync();
            var fileListResult = await fileListTask;

            if (fileListResult == null || fileListResult.Files.Any() == false)
                return;

            var jobListResult = await jobListTask;
            var jobs = jobListResult?.Jobs ?? new List<Job>();
            var files = fileListResult.Files;

            foreach (var file in files)
            {
                if (DateTime.UnixEpoch.AddSeconds(file.Modified) > _purgeBefore)
                    continue;

                if (jobs.Any(x => x.Path == file.Path) && _excludeQueued)
                    continue;

                // if (jobs.Any(x => x.Path == file.Path) && await _moonrakerClient.DeleteJob())
                // {

                // }
                // Delete file
            }

            return;
        }

        public bool ProcessFile(Moonraker.File file)
        {
            var lastModified = DateTime.UnixEpoch.AddSeconds(file.Modified);

            if (lastModified > _purgeBefore)
                return false;



            return false;
        }
    }
}