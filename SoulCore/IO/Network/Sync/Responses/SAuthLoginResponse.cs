using SoulCore.Game.Enums;

namespace SoulCore.IO.Network.Sync.Responses
{
    public sealed record SAuthLoginResponse
    {
        public int AccountId { get; init; }
        public bool IsClearTutorial { get; init; }
        public string Mac { get; init; } = MacEmpty;
        public string ErrorMessage { get; init; } = string.Empty;
        public AuthLoginErrorMessageCode ErrorCode { get; init; }
        public byte LoginType { get; init; }
        public string AuthId { get; init; } = string.Empty;
        public ulong SessionKey { get; init; }
        public byte GameMasterPower { get; init; }
        public ushort BrithYear { get; init; }
        public byte BrithMonth { get; init; }
        public byte BrithDay { get; init; }

        public static string MacEmpty => "00-00-00-00-00-00";
    }
}
