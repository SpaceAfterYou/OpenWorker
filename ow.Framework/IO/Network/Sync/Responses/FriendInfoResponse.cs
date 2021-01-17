using ow.Framework.Game.Enums;
using System;
using System.Collections.Generic;

namespace ow.Framework.IO.Network.Sync.Responses
{
    public sealed record FriendInfoResponse
    {
        public sealed record Entity
        {
            public string Name { get; init; } = string.Empty;
            public int Id { get; init; }
            public byte Level { get; init; }
            public Hero Hero { get; init; }
            public byte Awaken { get; init; }
            public uint Photo { get; init; }
            public byte Type { get; init; }
            public byte State { get; init; }
            public string Memo { get; init; } = string.Empty;
            public byte Channel { get; init; }
            public uint Location { get; init; }
            public ulong Point { get; init; }
            public bool Login { get; init; }
            public long LogOut { get; init; }
            public long Remain { get; init; }
        }

        public IEnumerable<Entity> Values { get; init; } = Array.Empty<Entity>();
    }
}