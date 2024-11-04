using Newtonsoft.Json;

namespace Klipper.Purge.Console.Moonraker
{
    public class DirectoryListResult
    {
        public List<Directory> Directories { get; set; }

        public List<File> Files { get; set; }

        public DirectoryListResult()
        {
            Directories = new List<Directory>();
            Files = new List<File>();
        }
    }
}