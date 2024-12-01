using Microsoft.Extensions.Configuration;

namespace Klipper.Purge.Console.Options
{
    public class FilePurgeOptions
    {
        [ConfigurationKeyName("FILE_PURGE_ENABLED")]
        public bool Enabled { get; init; } = true;

        [ConfigurationKeyName("FILE_PURGE_RUN_ON_STARTUP")]
        public bool RunOnStartup { get; init; } = true;

        [ConfigurationKeyName("FILE_PURGE_SCHEDULE")]
        public string Schedule { get; init; } = "0 0 3 ? * *";

        [ConfigurationKeyName("FILE_PURGE_OLDER_THAN")]
        public int PurgeOlderThanDays { get; init; } = 7;

        [ConfigurationKeyName("FILE_PURGE_EXCLUDE_QUEUED")]
        public bool ExcludeQueued { get; init; } = true;
    }
}