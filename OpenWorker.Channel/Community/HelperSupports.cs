using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Persistent;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Modules.Helpers;
using OpenWorker.Gameplay.Modules.Helpers.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Helpers.Responses;
using OpenWorker.Hotspot.Modules.Helpers.Types;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Persistence;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Channel.Community;

/// <summary>
/// The orbs of friends. A person offers its friends an orb of attack or of defense, worth a fifth of its own physical attack
/// or defense, for a day, and takes a reward for it; a friend who has not used it yet sees it in its list and puts it on one of
/// its helpers, where it adds its value until the helper is sent away from a maze, the maze is cleared, or the day ends. The
/// orbs and who used them are rows of the database, so every server sees them and they outlast a restart.
/// </summary>
public sealed class HelperSupports(
    World world,
    IDbContextFactory<PersistenceContext> factory,
    HelperGear gear,
    InventoryService inventory,
    PlayerStatsService stats,
    ReadOnlyCollection<HelperRewardRow> rewards,
    TimeProvider time)
{
    /// <summary>
    /// The reward of the last orb is still to be taken: a new one can be offered after.
    /// </summary>
    public const int RewardWaiting = 58003;

    /// <summary>
    /// The orb could not be offered: its kind is unknown, or the last one is still valid.
    /// </summary>
    public const int NotRegistered = 1;

    /// <summary>
    /// The orb of the friend is past its day (the original's result).
    /// </summary>
    public const int OrbExpired = 1;

    /// <summary>
    /// The friend offers no orb (the original's result), or is not a friend.
    /// </summary>
    public const int NoOrb = 2;

    /// <summary>
    /// The person used this orb already.
    /// </summary>
    public const int OrbUsed = 3;

    /// <summary>
    /// There is no reward to take.
    /// </summary>
    public const ushort NoReward = 52004;

    /// <summary>
    /// The bag has no room for the reward.
    /// </summary>
    public const ushort NoRoom = 52010;

    /// <summary>
    /// A fifth of the stat of the person is what its orb adds.
    /// </summary>
    public const float Share = 0.2f;

    /// <summary>
    /// How long an orb can be used.
    /// </summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    // The reward for offering an orb, and the one for each use of it.
    private const byte OfferReward = 1;
    private const byte UseReward = 2;

    // A friend more than this many levels above the reader gives an orb scaled down to the reader's level.
    private const int LevelGap = 5;

    // The client keeps 20 characters of a name.
    private const int NameLength = 20;

    private readonly RowIndex<int, HelperRewardRow> _rewards = rewards.IndexBy(e => (int)e.Id);

    /// <summary>
    /// What a friend more than five levels above the reader gives: the value is cut down in proportion to the levels.
    /// </summary>
    public static float Scaled(float value, int friendLevel, int readerLevel)
    {
        return friendLevel > readerLevel && friendLevel - readerLevel > LevelGap && friendLevel > 0 ? value / friendLevel * readerLevel : value;
    }

    /// <summary>
    /// The orb the person offers: registered while its day lasts, shown with its reward until that is taken.
    /// </summary>
    public async ValueTask SendInfoAsync(Entity player)
    {
        if (!TryGetPerson(player, out var person))
        {
            return;
        }

        var support = await factory.SupportOfAsync(person);

        Send(player, new HelperSupportInfoResponse { Info = Info(person, support) });
    }

    public async ValueTask RegisterAsync(Entity player, byte supportType)
    {
        if (!TryGetPerson(player, out var person))
        {
            return;
        }

        var own = stats.Of(player).Physical;
        var value = supportType switch
        {
            HelperStats.AttackOrb => own.AttackMax * Share,
            HelperStats.DefenseOrb => own.Defense * Share,
            _ => -1f
        };

        if (value < 0)
        {
            Send(player, new HelperSupportRegisterResponse { Result = NotRegistered, Info = Info(person, null) });
            return;
        }

        var now = time.GetUtcNow();
        var until = now + Lifetime;

        var outcome = await factory.RegisterSupportAsync(person, supportType, value, until.UtcDateTime, now.UtcDateTime);

        var support = outcome == SupportRegisterOutcome.Registered
            ? new HelperSupportPersistent
            {
                PersonId = person,
                SupportType = supportType,
                Value = value,
                Until = until.UtcDateTime,
                Reward = HelperSupportPersistent.RewardWaiting
            }
            : null;

        Send(player, new HelperSupportRegisterResponse
        {
            Result = outcome switch
            {
                SupportRegisterOutcome.Registered => 0,
                SupportRegisterOutcome.RewardWaiting => RewardWaiting,
                _ => NotRegistered
            },
            Info = Info(person, support)
        });
    }

    /// <summary>
    /// The reward for offering an orb, taken once: the items of the table, if the bag has room for them.
    /// </summary>
    public async ValueTask RewardAsync(Entity player)
    {
        if (!TryGetPerson(player, out var person))
        {
            return;
        }

        var items = Items(OfferReward);
        var support = await factory.SupportOfAsync(person);

        if (!world.IsAlive(player))
        {
            return;
        }

        if (support?.Reward != HelperSupportPersistent.RewardWaiting || !_rewards.TryGet(OfferReward, out _))
        {
            Refuse(player, HelperOpcode.SupportReward, NoReward);
            return;
        }

        if (!inventory.CanGiveAll(player, items))
        {
            Refuse(player, HelperOpcode.SupportReward, NoRoom);
            return;
        }

        if (!await factory.ClaimSupportRewardAsync(person, time.GetUtcNow().UtcDateTime))
        {
            Refuse(player, HelperOpcode.SupportReward, NoReward);
            return;
        }

        // The bag may have filled while the claim was written: then the reward waits for another try.
        if (!world.IsAlive(player) || !inventory.CanGiveAll(player, items))
        {
            await factory.ReturnSupportRewardAsync(person);

            Refuse(player, HelperOpcode.SupportReward, NoRoom);
            return;
        }

        foreach (var (item, count) in items)
        {
            inventory.TryGive(player, item, count);
        }

        Send(player, new HelperSupportRewardResponse { Result = 0 });
    }

    /// <summary>
    /// The orbs the friends of the person offer and it has not used, each worth what it gives the person.
    /// </summary>
    public async ValueTask SendListAsync(Entity player)
    {
        if (!TryGetPerson(player, out var person))
        {
            return;
        }

        var offers = await factory.SupportOffersAsync(person, time.GetUtcNow().UtcDateTime);

        if (!world.IsAlive(player))
        {
            return;
        }

        var level = LevelOf(player);

        Send(player, new HelperSupportListResponse
        {
            Friends = offers
                .Select(e => new HelperSupportFriendValue(
                    e.Friend,
                    e.SupportType,
                    Scaled(e.Value, e.Level, level),
                    Utc(e.Until),
                    e.Value,
                    e.Name.Length > NameLength ? e.Name[..NameLength] : e.Name,
                    e.Level,
                    e.Hero))
                .ToArray()
        });
    }

    /// <summary>
    /// Puts the orb of a friend on a helper of the person, once per orb and person. The friend earns the points of the table
    /// with the person.
    /// </summary>
    public async ValueTask EquipAsync(Entity player, int helper, int friend)
    {
        if (!TryGetPerson(player, out var person) || !Owns(player, helper))
        {
            Refuse(player, HelperOpcode.SupportEquip, HelperGear.CannotEquip);
            return;
        }

        var points = _rewards.TryGet(UseReward, out var reward) ? reward.FriendPoints : 0;
        var (outcome, offer) = await factory.UseSupportAsync(person, friend, points, time.GetUtcNow().UtcDateTime);

        if (!world.IsAlive(player) || !Owns(player, helper))
        {
            return;
        }

        if (outcome != SupportUseOutcome.Used)
        {
            var result = outcome switch
            {
                SupportUseOutcome.Expired => OrbExpired,
                SupportUseOutcome.AlreadyUsed => OrbUsed,
                _ => NoOrb
            };

            Send(player, new HelperSupportEquipResponse { Result = result, Helper = helper, Record = RecordOf(player, helper) });
            return;
        }

        var orb = new HelperOrb
        {
            Friend = friend,
            SupportType = offer.SupportType,
            Value = Scaled(offer.Value, offer.Level, LevelOf(player)),
            Until = Utc(offer.Until)
        };

        gear.WearOrb(player, helper, orb, out var record);

        Send(player, new HelperSupportEquipResponse { Result = 0, Helper = helper, Record = record });
    }

    private HelperSupportInfoValue Info(int person, HelperSupportPersistent? support)
    {
        if (support is null)
        {
            return new HelperSupportInfoValue(0, 0, 0, DateTimeOffset.UnixEpoch, 0, false);
        }

        var valid = Utc(support.Until) > time.GetUtcNow();

        // An orb whose day is over and whose reward was taken is gone; one whose reward waits stays to be taken.
        return valid || support.Reward == HelperSupportPersistent.RewardWaiting
            ? new HelperSupportInfoValue(person, support.SupportType, support.Value, Utc(support.Until), support.Reward, valid)
            : new HelperSupportInfoValue(0, 0, 0, DateTimeOffset.UnixEpoch, 0, false);
    }

    private (int Item, int Count)[] Items(byte reward)
    {
        if (!_rewards.TryGet(reward, out var row))
        {
            return [];
        }

        (int Item, int Count)[] items = [(row.Item1, row.Count1), (row.Item2, row.Count2), (row.Item3, row.Count3)];

        return items.Where(e => e.Item != 0 && e.Count > 0).ToArray();
    }

    private HelperValue RecordOf(Entity player, int helper)
    {
        var entry = world.Get<HelperComponent>(player).Book!.Find(helper)!;

        return gear.Record(player, entry);
    }

    private bool Owns(Entity player, int helper)
    {
        return world.IsAlive(player) && world.TryGet<HelperComponent>(player, out var component) && component.Book?.Find(helper) is not null;
    }

    private bool TryGetPerson(Entity player, out int person)
    {
        person = 0;

        if (!world.IsAlive(player) || !world.TryGet<HelperComponent>(player, out var component) || component.Book is null)
        {
            return false;
        }

        person = component.Person;

        return true;
    }

    private byte LevelOf(Entity player)
    {
        return world.TryGet<LevelComponent>(player, out var reached) ? reached.Level : (byte)1;
    }

    private static DateTimeOffset Utc(DateTime value)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    private void Send<T>(Entity player, T message) where T : IResponseHotspotMessage
    {
        if (world.IsAlive(player) && world.Has<ServerSessionComponent>(player))
        {
            world.Get<ServerSessionComponent>(player).Send(message);
        }
    }

    private void Refuse(Entity player, HelperOpcode command, ushort code)
    {
        Send(player, new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Helper, command), code));
    }
}
