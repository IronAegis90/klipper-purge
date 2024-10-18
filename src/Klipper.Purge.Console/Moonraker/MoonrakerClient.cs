using System.Net;
using Klipper.Purge.Console.Moonraker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

        public async Task<List<string>> DirectoriesAsync()
        {
            var response = await _httpClient.GetAsync("/server/files/list?root=gcodes");

            if (response.IsSuccessStatusCode == false)
                return null;

            var test = await response.Content.ReadAsStringAsync();

            return null;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}