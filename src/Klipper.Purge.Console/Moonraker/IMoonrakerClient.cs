namespace Klipper.Purge.Console.Moonraker
{
    public interface IMoonrakerClient
    {
        Task<PrintStatus?> GetPrintStatusAsync();

        Task<DirectoryListResult?> ListDirectoriesAsync(string parent);

        Task<JobListResult?> ListJobsAsync();

        Task<bool> DeleteFileAsync(string path);

        Task<bool> DeleteJobAsync(string id);

        Task<bool> DeleteDirectoryAsync(string path);
    }
}