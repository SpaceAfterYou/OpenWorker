using ow.Framework.IO.Network.Sync.Responses.Shared;

namespace ow.Framework.IO.Network.Sync.Responses
{
    public sealed record SGateCharacterSelectResponse
    {
        public int AccountId { get; init; }
        public int CharacterId { get; init; }
        public uint ServerId { get; init; }
        public uint JumpId { get; init; }
        public uint PortalId { get; init; }
        public SEndPointSharedResponse EndPoint { get; init; } = SEndPointSharedResponse.Empty;
        public SMapInfoSharedResponse Map { get; init; }
        public SMapInfoSharedResponse ParentMap { get; init; }
        public SPosInfoSharedResponse Pos { get; init; } = SPosInfoSharedResponse.Empty;
        public byte Type { get; init; }
    }
}