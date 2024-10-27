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

        public async Task<FileListResult?> ListFilesAsync()
        {
            var response = await _httpClient.GetAsync("/server/files/list?root=gcodes");

            if (response.IsSuccessStatusCode == false)
                return null;

            var result = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<FileListResult>(result);
        }

        public async Task<JobListResult?> ListJobsAsync()
        {
            var response = await _httpClient.GetAsync("/server/job_queue/status");

            if (response.IsSuccessStatusCode == false)
                return null;

            var result = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<JobListResult>(result);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}