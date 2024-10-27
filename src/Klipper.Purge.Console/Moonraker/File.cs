using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Klipper.Purge.Console.Moonraker
{
    public class File
    {
        public required string Path { get; set; }


        public required float Modified { get; set; }
    }
}