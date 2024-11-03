using Newtonsoft.Json;

namespace Klipper.Purge.Console.Moonraker
{
    public class PrintStatusResult
    {
        [JsonProperty("result.status.print_stats.filename")]
        public required string Filename { get; set; }

        [JsonProperty("result.status.print_stats.state")]
        public required string State { get; set; }
    }
}