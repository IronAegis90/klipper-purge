using Newtonsoft.Json;

namespace Klipper.Purge.Console.Moonraker
{
    public class FileListResult
    {
        [JsonProperty("result")]
        public List<File> Files { get; set; }

        public FileListResult()
        {
            Files = new List<File>();
        }
    }
}