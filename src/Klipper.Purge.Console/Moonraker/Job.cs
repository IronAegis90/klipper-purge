using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace Klipper.Purge.Console.Moonraker
{
    public class Job
    {
        [JsonPropertyName("job_id")]
        public required string Id { get; set; }

        [JsonPropertyName("filename")]
        public required string Path { get; set; }

        [JsonPropertyName("time_added")]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public required DateTime AddedOn { get; set; }
    }
}