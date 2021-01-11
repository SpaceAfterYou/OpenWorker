using Grpc.Core;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ow.Service.World.Game
{
    public sealed class Service : Channel
    {
        public Service(string target, ChannelCredentials credentials) : base(target, credentials)
        {
        }
    }

    public sealed class CharacterParty
    {
        public ConcurrentDictionary<int, PartyRepository.Entity> OutcomingInvites = new();
        public ConcurrentDictionary<int, PartyRepository.Entity> IncomingInvites = new();

        public PartyRepository.Entity? Entity { get; set; }
    }

    public sealed class Character
    {
        public int Id { get; }
        public object PartyLock { get; } = new();

        public bool IsInParty => Party.Entity is not null;
        public bool IsNotInParty => !IsInParty;

        public CharacterParty Party { get; } = new();
    }

    public sealed class PartyRepository
    {
        public sealed class Entity
        {
            public ConcurrentDictionary<int, Character> Characters = new();
            public ConcurrentDictionary<int, Character> OutcomingInvites = new();
            public ConcurrentDictionary<int, Character> IncomingInvites = new();

            public int Id { get; }
            public byte MaxCharacters { get; }
            public object Lock { get; } = new();
            public Character Master { get; set; }

            public bool TryInvite(Character character)
            {
                lock (Lock)
                {
                    if (MaxCharacters <= Characters.Count)
                        return false;

                    return OutcomingInvites.TryAdd(character.Id, character);
                }
            }

            public Entity(byte maxCharacters, Character master)
            {
                Id = GetNextId();
                MaxCharacters = maxCharacters;
                Master = master;
            }

            private static int GetNextId()
            {
                lock (_nextIdLock)
                {
                    if (_nextIdGenerator.MoveNext())
                        return _nextIdGenerator.Current;
                }

                return -1;
            }

            private static IEnumerable<int> NextIdGenerator()
            {
                for (int i = 0; ; ++i)
                    yield return i;
            }

            private static readonly object _nextIdLock = new { };
            private static readonly IEnumerator<int> _nextIdGenerator = NextIdGenerator().GetEnumerator();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="type">4 Party, 8 - PartyEx (aka SoulSquad)</param>
        /// <param name="master"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public bool Invite(byte type, Character master, Character member)
        {
            lock (member.PartyLock)
            {
                if (member.IsInParty)
                    return false;

                lock (master.PartyLock)
                {
                    if (master.IsNotInParty)
                    {
                        master.Party.Entity = new(type, master);
                        if (!_party.TryAdd(master.Party.Entity.Id, master.Party.Entity) || !master.Party.Entity.Characters.TryAdd(master.Id, master))
                        {
                            master.Party.Entity = null;
                            return false;
                        }
                    }

                    return master.Party.Entity!.TryInvite(member);
                }
            }
        }

        private readonly ConcurrentDictionary<int, Character> _playerInParty = new();
        private readonly ConcurrentDictionary<int, Entity> _party = new();
    }
}