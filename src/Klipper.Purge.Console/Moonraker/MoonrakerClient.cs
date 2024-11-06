using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz.Impl.AdoJobStore;

namespace Klipper.Purge.Console.Moonraker
{
    public class MoonrakerClient : IMoonrakerClient, IDisposable
    {
        private readonly ILogger<MoonrakerClient> _logger;

        private readonly HttpClient _httpClient;

        public MoonrakerClient(ILogger<MoonrakerClient> logger, IConfiguration configuration)
        {
            _logger = logger;
            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri(configuration.GetValue<string>("MOONRAKER_URL") ?? throw new InvalidConfigurationException())
            };
        }

        public async Task<PrintStatus?> GetPrintStatusAsync()
        {
            var response = await _httpClient.GetAsync("/printer/objects/query?print_stats");

            if (response.IsSuccessStatusCode == false)
                return null;

            var result = await response.Content.ReadAsStringAsync();

            dynamic root = JObject.Parse(result);

            return new PrintStatus()
            {
                Filename = root.result.status.print_stats.filename,
                State = root.result.status.print_stats.state
            };
        }

        public async Task<DirectoryListResult?> ListDirectoriesAsync(string parent)
        {
            var response = await _httpClient.GetAsync($"/server/files/directory?path={parent}");

            if (response.IsSuccessStatusCode == false)
                return null;

            var result = await response.Content.ReadAsStringAsync();
            var root = JObject.Parse(result);
            var directories = root["result"]?["dirs"]?.ToString() ?? string.Empty;
            var files = root["result"]?["files"]?.ToString() ?? string.Empty;

            return new DirectoryListResult()
            {
                Directories = JsonConvert.DeserializeObject<List<Directory>>(directories) ?? new List<Directory>(),
                Files = JsonConvert.DeserializeObject<List<File>>(files) ?? new List<File>()
            };
        }

        public async Task<JobListResult?> ListJobsAsync()
        {
            var response = await _httpClient.GetAsync("/server/job_queue/status");

            if (response.IsSuccessStatusCode == false)
                return null;

            var result = await response.Content.ReadAsStringAsync();
            var root = JObject.Parse(result);
            var jobs = root["result"]?["queued_jobs"]?.ToString() ?? string.Empty;

            return new JobListResult()
            {
                Jobs = JsonConvert.DeserializeObject<List<Job>>(jobs) ?? new List<Job>()
            };
        }

        public async Task<bool> DeleteFileAsync(string path)
        {
            var response = await _httpClient.DeleteAsync($"/server/files/gcodes/{path}");

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteJobAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"/server/job_queue/job?job_ids={id}");

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteDirectoryAsync(string path)
        {
            var response = await _httpClient.DeleteAsync($"/server/files/directory?path={path}&force=false");

            return response.IsSuccessStatusCode;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}