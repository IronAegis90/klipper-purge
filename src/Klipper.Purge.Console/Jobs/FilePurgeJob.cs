using System.Security.Cryptography.X509Certificates;
using Klipper.Purge.Console.Moonraker;
using Klipper.Purge.Console.Options;
using Microsoft.Extensions.Configuration;
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

        public readonly DateTime _purgeBefore;

        public readonly bool _excludeQueued;

        public FilePurgeJob(ILogger<FilePurgeJob> logger, IOptions<FilePurgeOptions> options, IMoonrakerClient moonrakerClient)
        {
            _logger = logger;
            _options = options;
            _moonrakerClient = moonrakerClient;
        }

        public static void Register(IConfiguration configuration, IServiceCollectionQuartzConfigurator quartz)
        {
            if (configuration.GetValue<bool>("Jobs:FilePurge:Enabled") == false)
                return;

            var jobKey = new JobKey("file-purge");

            quartz.AddJob<FilePurgeJob>(jobKey, job => job.WithDescription(""));

            if (configuration.GetValue<bool>("Jobs:FilePurge:RunOnStartup"))
                quartz.AddTrigger(trigger => trigger.ForJob(jobKey).StartNow());

            var schedule = configuration.GetValue<string>("Jobs:FilePurge:Schedule");

            if (schedule is null || CronExpression.IsValidExpression(schedule) == false)
                return;

            quartz.AddTrigger(trigger => trigger.ForJob(jobKey).WithCronSchedule(schedule));
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
                if (file.Modified > _purgeBefore)
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
    }
}