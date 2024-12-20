using Newtonsoft.Json;

namespace Klipper.Purge.Console.Moonraker
{
    public class File
    {
        [JsonProperty("filename")]
        public required string Name { get; set; }

        public required float Modified { get; set; }
    }
}