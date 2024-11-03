using Newtonsoft.Json;

namespace Klipper.Purge.Console.Moonraker
{
    public class JobQueueResult
    {
        [JsonProperty("queued_jobs")]
        public List<Job> Jobs { get; set; }

        public JobQueueResult()
        {
            Jobs = new List<Job>();
        }
    }
}