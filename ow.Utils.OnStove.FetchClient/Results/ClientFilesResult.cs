using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ow.Utils.FetchOnStoveClient.Results
{
    public sealed record ClientFilesResult
    {
        [JsonPropertyName("service_code")]
        public string ServiceCode { get; init; } = string.Empty;

        [JsonPropertyName("service_name")]
        public string ServiceName { get; init; } = string.Empty;

        [JsonPropertyName("version_no")]
        public int VersionNo { get; init; }

        [JsonPropertyName("root_folder")]
        public string RootFolder { get; init; } = string.Empty;

        [JsonPropertyName("execution")]
        public string Execution { get; init; } = string.Empty;

        [JsonPropertyName("extract_size")]
        public string ExtractSize { get; init; } = string.Empty;

        [JsonPropertyName("packed_size")]
        public string PackedSize { get; init; } = string.Empty;

        [JsonPropertyName("directx")]
        public string DirectX { get; init; } = string.Empty;

        [JsonPropertyName("vcredist")]
        public string VCRedist { get; init; } = string.Empty;

        [JsonPropertyName("vcredist_2")]
        public string VCRedist2 { get; init; } = string.Empty;

        [JsonPropertyName("vcredist_3")]
        public string VCRedist3 { get; init; } = string.Empty;

        [JsonPropertyName("vcredist_4")]
        public string VCRedist4 { get; init; } = string.Empty;

        [JsonPropertyName("vcredist_lang_1")]
        public string VCRedistLang1 { get; init; } = string.Empty;

        [JsonPropertyName("vcredist_lang_2")]
        public string VCRedistLang2 { get; init; } = string.Empty;

        [JsonPropertyName("vcredist_lang_3")]
        public string VCRedistLang3 { get; init; } = string.Empty;

        [JsonPropertyName("vcredist_lang_4")]
        public string VCRedistLang4 { get; init; } = string.Empty;

        [JsonPropertyName("addons")]
        public string Addons { get; init; } = string.Empty;

        [JsonPropertyName("dotnet")]
        public string DotNet { get; init; } = string.Empty;

        [JsonPropertyName("eula")]
        public string Eula { get; init; } = string.Empty;

        [JsonPropertyName("migration_reg")]
        public IReadOnlyList<object> MigrationReg { get; init; } = Array.Empty<object>();

        [JsonPropertyName("migration_path")]
        public string MigrationPath { get; init; } = string.Empty;

        [JsonPropertyName("startpos")]
        public string StartPos { get; init; } = string.Empty;

        [JsonPropertyName("startpos_type")]
        public string StartPosType { get; init; } = string.Empty;

        [JsonPropertyName("light_seq")]
        public string LightSeq { get; init; } = string.Empty;

        [JsonPropertyName("files")]
        public IReadOnlyList<string> Files { get; init; } = Array.Empty<string>();
    }
}