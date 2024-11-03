using Newtonsoft.Json;

namespace Klipper.Purge.Console.Moonraker
{
    public class PrinterStatus
    {
        [JsonProperty("print_stats")]
        public PrintStatus PrintStatus { get; set; }
    }
}