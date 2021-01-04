using ow.Utils.FetchOnStoveClient.Results;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ow.Utils.FetchOnStoveClient
{
    public static partial class OnStove
    {
        public static async ValueTask<GetLiveVersionResult?> GetLiveVersion()
        {
            using HttpRequestMessage requestMessage = new(HttpMethod.Post, $"http://patchapi.onstove.com/{_apiVersion}/get_live_version");
            requestMessage.Headers.Host = requestMessage.RequestUri?.Host;
            requestMessage.Headers.AcceptLanguage.ParseAdd("en-US,*");
            requestMessage.Headers.UserAgent.ParseAdd("Mozilla/5.0");
            requestMessage.Content = new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>>()
                {
                    new("local_version", "0"),
                    new("service_code", SoulWorkerGameCode)
                });

            using HttpClient client = new();
            using HttpResponseMessage response = (await client.SendAsync(requestMessage)).EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GetLiveVersionResult?>();
        }

        public static async ValueTask<ClientFilesResult?> GetClientFiles(string? host, ushort port)
        {
            GetLiveVersionResult? liveVersion = await GetLiveVersion();
            using HttpRequestMessage request = new(HttpMethod.Get, liveVersion?.Value?.LiveProjectUrl);

            using HttpClient http = new(new HttpClientHandler()
            {
                Proxy = host is not null ? new WebProxy(host, port) : null
            }, true);

            using HttpResponseMessage response = (await http.SendAsync(request)).EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ClientFilesResult?>();
        }

        public const string SoulWorkerGameCode = "11";
        private const string _apiVersion = "apiv1";
    }
}