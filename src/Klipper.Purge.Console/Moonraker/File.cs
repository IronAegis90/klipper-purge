using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace Klipper.Purge.Console.Moonraker
{
    public class File
    {
        public required string Path { get; set; }

        [JsonConverter(typeof(UnixDateTimeConverter))]
        public required DateTime Modified { get; set; }
    }
}