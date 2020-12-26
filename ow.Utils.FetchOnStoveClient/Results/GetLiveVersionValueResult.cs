using System.Text.Json.Serialization;

namespace ow.Utils.FetchOnStoveClient.Results
{
    public sealed record GetLiveVersionValueResult
    {
        [JsonPropertyName("pid")]
        public string Pid { get; init; }

        [JsonPropertyName("service_code")]
        public string ServiceCode { get; init; }

        [JsonPropertyName("live_version")]
        public int LiveVersion { get; init; }

        [JsonPropertyName("live_project_url")]
        public string LiveProjectUrl { get; init; }

        [JsonPropertyName("local_version")]
        public int LocalVersion { get; init; }

        [JsonPropertyName("local_project_url")]
        public string LocalProjectUrl { get; init; }
    }
}