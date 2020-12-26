using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ow.Utils.FetchOnStoveClient.Results
{
    public sealed record ClientFilesResult
    {
        [JsonPropertyName("service_code")]
        public string ServiceCode { get; init; }

        [JsonPropertyName("service_name")]
        public string ServiceName { get; init; }

        [JsonPropertyName("version_no")]
        public int VersionNo { get; init; }

        [JsonPropertyName("root_folder")]
        public string RootFolder { get; init; }

        [JsonPropertyName("execution")]
        public string Execution { get; init; }

        [JsonPropertyName("extract_size")]
        public string ExtractSize { get; init; }

        [JsonPropertyName("packed_size")]
        public string PackedSize { get; init; }

        [JsonPropertyName("directx")]
        public string DirectX { get; init; }

        [JsonPropertyName("vcredist")]
        public string VCRedist { get; init; }

        [JsonPropertyName("vcredist_2")]
        public string VCRedist2 { get; init; }

        [JsonPropertyName("vcredist_3")]
        public string VCRedist3 { get; init; }

        [JsonPropertyName("vcredist_4")]
        public string VCRedist4 { get; init; }

        [JsonPropertyName("vcredist_lang_1")]
        public string VCRedistLang1 { get; init; }

        [JsonPropertyName("vcredist_lang_2")]
        public string VCRedistLang2 { get; init; }

        [JsonPropertyName("vcredist_lang_3")]
        public string VCRedistLang3 { get; init; }

        [JsonPropertyName("vcredist_lang_4")]
        public string VCRedistLang4 { get; init; }

        [JsonPropertyName("addons")]
        public string Addons { get; init; }

        [JsonPropertyName("dotnet")]
        public string DotNet { get; init; }

        [JsonPropertyName("eula")]
        public string Eula { get; init; }

        [JsonPropertyName("migration_reg")]
        public IReadOnlyList<object> MigrationReg { get; init; }

        [JsonPropertyName("migration_path")]
        public string MigrationPath { get; init; }

        [JsonPropertyName("startpos")]
        public string StartPos { get; init; }

        [JsonPropertyName("startpos_type")]
        public string StartPosType { get; init; }

        [JsonPropertyName("light_seq")]
        public string LightSeq { get; init; }

        [JsonPropertyName("files")]
        public IReadOnlyList<string> Files { get; init; }
    }
}