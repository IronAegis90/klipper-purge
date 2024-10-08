using System;
using FluentScheduler;
using Microsoft.Extensions.Configuration;

namespace Klipper.Purge.Console
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            JobManager.Initialize(new JobRegistry());
        }
    }
}