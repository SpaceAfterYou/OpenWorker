using SoulCore.Game.Enums;

namespace SoulCore.IO.Network.Sync.Requests.Force
{
    public abstract record SRFMember
    {
        public int Member { get; }
        public string Name { get; } = string.Empty;
        public byte Level { get; }
        public Hero Hero { get; }
        public byte Awaken { get; }
        public uint Photo { get; }
        public ushort Location { get; }

        //internal protected SRFMember()
        //{
        //}
    }
}
