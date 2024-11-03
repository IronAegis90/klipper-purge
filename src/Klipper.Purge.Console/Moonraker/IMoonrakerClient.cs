namespace Klipper.Purge.Console.Moonraker
{
    public interface IMoonrakerClient
    {
        Task<PrintStatusResult?> GetPrintStatusAsync();

        Task<FileListResult?> ListFilesAsync();

        Task<JobListResult?> ListJobsAsync();

        Task<bool> DeleteFileAsync(string path);

        Task<bool> DeleteJobAsync(string id);
    }
}