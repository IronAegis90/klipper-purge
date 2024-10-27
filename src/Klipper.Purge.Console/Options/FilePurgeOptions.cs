using System.ComponentModel.DataAnnotations;

namespace Klipper.Purge.Console.Options
{
    public class FilePurgeOptions
    {
        public bool Enabled { get; init; } = true;

        public bool RunOnStartup { get; init; } = true;

        public string Schedule { get; init; } = "0 0 3 * * * *";

        public int PurgeOlderThanDays { get; init; } = 7;

        public bool ExcludeQueued { get; init; } = true;
    }
}