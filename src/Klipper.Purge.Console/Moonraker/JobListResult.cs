using Newtonsoft.Json;

namespace Klipper.Purge.Console.Moonraker
{
    public class JobListResult
    {
        public List<Job> Jobs { get; set; }

        public JobListResult()
        {
            Jobs = new List<Job>();
        }
    }
}