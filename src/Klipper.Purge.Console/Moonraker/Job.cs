using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Klipper.Purge.Console.Moonraker
{
    public class Job
    {
        [JsonProperty("job_id")]
        public required string Id { get; set; }

        [JsonProperty("filename")]
        public required string Path { get; set; }

        [JsonProperty("time_added")]
        public required double AddedOn { get; set; }
    }
}