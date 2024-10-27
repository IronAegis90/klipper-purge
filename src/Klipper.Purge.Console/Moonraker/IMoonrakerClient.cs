namespace Klipper.Purge.Console.Moonraker
{
    public interface IMoonrakerClient
    {
        Task<FileListResult?> ListFilesAsync();

        Task<JobListResult?> ListJobsAsync();
    }
}