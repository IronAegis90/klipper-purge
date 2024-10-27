using Newtonsoft.Json;

namespace Klipper.Purge.Console.Moonraker
{
    public class JobListResult
    {
        [JsonProperty("result.queued_jobs")]
        public List<Job> Jobs { get; set; }

        public JobListResult()
        {
            Jobs = new List<Job>();
        }
    }
}