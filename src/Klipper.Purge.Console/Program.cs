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

            builder.Services.AddOptions<FilePurgeOptions>().BindConfiguration("Jobs:FilePurge").ValidateOnStart();

            builder.Services.AddQuartz(x =>
            {
                x.SchedulerName = "Klipper Purge";

                FilePurgeJob.Register(configuration, x);
            });

            builder.Services.AddQuartzHostedService(x =>
            {
                x.WaitForJobsToComplete = true;
            });

            builder.Services.AddSingleton<IMoonrakerClient, MoonrakerClient>();

            builder.Logging.AddConsole();

            var app = builder.Build();

            app.Run();
        }
    }
}