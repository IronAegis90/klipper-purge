using Newtonsoft.Json;

namespace Klipper.Purge.Console.Moonraker
{
    public class PrintStatus
    {
        public required string Filename { get; set; }

        public required string State { get; set; }
    }
}