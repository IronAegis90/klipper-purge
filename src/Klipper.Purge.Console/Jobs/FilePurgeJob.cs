using System.Security.Cryptography.X509Certificates;
using Klipper.Purge.Console.Moonraker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Klipper.Purge.Console.Jobs
{
    public class FilePurgeJob : IJob
    {
        private readonly ILogger<FilePurgeJob> _logger;

        private readonly IMoonrakerClient _moonrakerClient;

        public FilePurgeJob(ILogger<FilePurgeJob> logger, IMoonrakerClient moonrakerClient)
        {
            _logger = logger;
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

        public Task Execute(IJobExecutionContext context)
        {
            _logger.LogDebug("Executing file purge");

            var rresult = _moonrakerClient.DirectoriesAsync();

            return Task.CompletedTask;
        }
    }
}