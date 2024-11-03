using System.Net;
using Klipper.Purge.Console.Moonraker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
                BaseAddress = new Uri(configuration.GetValue<string>("Moonraker:Url") ?? throw new InvalidConfigurationException())
            };
        }

        public async Task<Printer?> GetPrinterStatusAsync()
        {
            var response = await _httpClient.GetAsync("/printer/objects/query?print_stats");

            if (response.IsSuccessStatusCode == false)
                return null;

            var result = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Printer>(result);
        }

        public async Task<FileListResult?> ListFilesAsync()
        {
            var response = await _httpClient.GetAsync("/server/files/list");

            if (response.IsSuccessStatusCode == false)
                return null;

            var result = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<FileListResult>(result);
        }

        public async Task<JobQueueStatus?> GetJobQueueStatusAsync()
        {
            var response = await _httpClient.GetAsync("/server/job_queue/status");

            if (response.IsSuccessStatusCode == false)
                return null;

            var result = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<JobQueueStatus>(result);
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

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}