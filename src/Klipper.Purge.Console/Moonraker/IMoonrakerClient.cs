namespace Klipper.Purge.Console.Moonraker
{
    public interface IMoonrakerClient
    {
        Task<List<string>> DirectoriesAsync();
    }
}