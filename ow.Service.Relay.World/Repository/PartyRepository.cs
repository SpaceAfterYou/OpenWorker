using Grpc.Core;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ow.Service.Relay.World.Repository
{
    public sealed class Service : Channel
    {
        public Service(string target, ChannelCredentials credentials) : base(target, credentials)
        {
        }
    }

    public sealed class CharacterParty
    {
        internal ConcurrentDictionary<int, PartyRepository.Entity> OutcomingInvites = new();
        internal ConcurrentDictionary<int, PartyRepository.Entity> IncomingInvites = new();

        internal PartyRepository.Entity? Entity { get; set; }
    }

    public sealed class Character
    {
        internal readonly int Id;
        internal readonly object PartyLock = new();

        internal bool IsInParty => Party.Entity is not null;
        internal bool IsNotInParty => !IsInParty;

        internal CharacterParty Party { get; } = new();

        public Character(int id) => Id = id;
    }

    public sealed class PartyRepository
    {
        public sealed class Entity
        {
            internal ConcurrentDictionary<int, Character> Characters = new();
            internal ConcurrentDictionary<int, Character> OutcomingInvites = new();
            internal ConcurrentDictionary<int, Character> IncomingInvites = new();

            internal readonly int Id;
            internal readonly byte MaxCharacters;
            internal readonly object Lock = new();
            internal Character Master { get; set; }

            internal bool TryInvite(Character character)
            {
                lock (Lock)
                {
                    if (MaxCharacters <= Characters.Count)
                    {
                        return false;
                    }

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

        private readonly ConcurrentDictionary<int, Character> _playerInParty = new();
        private readonly ConcurrentDictionary<int, Entity> _party = new();

        /// <summary>
        ///
        /// </summary>
        /// <param name="type">4 Party, 8 - PartyEx (aka SoulSquad)</param>
        /// <param name="master"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        internal bool Invite(byte type, Character master, Character member)
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
    }
}