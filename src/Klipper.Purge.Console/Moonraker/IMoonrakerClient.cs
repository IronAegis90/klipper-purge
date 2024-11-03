namespace Klipper.Purge.Console.Moonraker
{
    public interface IMoonrakerClient
    {
        Task<Printer?> GetPrinterStatusAsync();

        Task<FileListResult?> ListFilesAsync();

        Task<JobQueueStatus?> GetJobQueueStatusAsync();

        Task<bool> DeleteFileAsync(string path);

        Task<bool> DeleteJobAsync(string id);
    }
}