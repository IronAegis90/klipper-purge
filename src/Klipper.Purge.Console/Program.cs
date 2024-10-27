using Klipper.Purge.Console.Jobs;
using Klipper.Purge.Console.Moonraker;
using Klipper.Purge.Console.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Klipper.Purge.Console
{

    public static class Program
    {
        private static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();
            var filePurgeSection = configuration.GetSection("Jobs:FilePurge");

            builder.Services.AddQuartz(quartz =>
            {
                quartz.SchedulerName = "Klipper Purge";

                FilePurgeJob.Register(filePurgeSection.Get<FilePurgeOptions>() ?? new FilePurgeOptions(), quartz);
            });

            builder.Services.AddQuartzHostedService(x =>
            {
                x.WaitForJobsToComplete = true;
            });

            builder.Services.AddOptions<FilePurgeOptions>().Bind(filePurgeSection);

            builder.Services.AddSingleton<IMoonrakerClient, MoonrakerClient>();

            builder.Logging.AddConsole();

            var app = builder.Build();

            app.Run();
        }
    }
}