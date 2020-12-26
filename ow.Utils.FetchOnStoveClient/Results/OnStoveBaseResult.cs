using System.Text.Json.Serialization;

namespace ow.Utils.FetchOnStoveClient.Results
{
    public abstract record OnStoveBaseResult<T>
    {
        [JsonPropertyName("message")]
        public string Message { get; init; }

        [JsonPropertyName("result")]
        public string Result { get; init; }

        [JsonPropertyName("value")]
        public T Value { get; init; }
    }
}