using Newtonsoft.Json;

namespace Klipper.Purge.Console.Moonraker
{
    public class Directory
    {
        [JsonProperty("dirname")]
        public required string Name { get; set; }
    }
}