using Arch.Core;
using OpenWorker.Gameplay.Modules.Persons.Extensions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Login;
using OpenWorker.Gameplay.Modules.Login.Extensions;
using OpenWorker.Gameplay.Modules.League.Components;
using OpenWorker.Gameplay.Modules.Maze.Components;
using OpenWorker.Gameplay.Modules.Maze.Extensions;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Hotspot.Messages.Response.Person.Enums;
using OpenWorker.Persistence;
using OpenWorker.Gameplay.Modules.Gestures.Components;
using OpenWorker.Hotspot.Modules.Gestures.Types;

namespace OpenWorker.Server.Common;

public sealed class PersonRegistry(World world, TimeProvider time)
{
    /// <summary>
    /// Sets what the person is from its row, and what its account keeps for it.
    /// </summary>
    public void PullPerson(Entity entity, PersonAtEntry entry)
    {
        var persistent = entry.Person;
        var accountOptions = entry.AccountOptions;

        world.Set(entity, new ActorComponent
        {
            Identifier = persistent.Id,
            Type = ActorType.User
        });

        // The lines about this session name the person from here on.
        world.SetLogPerson(entity, persistent.Id);

        world.Set(entity, new AbilityComponent
        {
            Health = new AbilityComponentEntry
            {
                Current = 100,
                Max = 100
            },
            SoulGain = new AbilityComponentEntry
            {
                Current = 100,
                Max = 100
            },
            SoulVapor = new AbilityComponentEntry
            {
                Current = 100,
                Max = 100
            },
            Stamina = new AbilityComponentEntry
            {
                Current = 100,
                Max = 100
            },
            SuperArmor = new AbilityComponentEntry
            {
                Current = 100,
                Max = 100
            },
            Speed = new SpeedComponentEntry
            {
                Attack = 100.0f,
                Move = 100.0f
            }
        });

        world.Set(entity, new AppearanceComponent
        {
            HairStyle = new AppearanceComponentEntry
            {
                Shape = persistent.DefaultHairStyle,
                Look = persistent.EquippedHairStyle
            },
            HairColor = new AppearanceComponentEntry
            {
                Shape = persistent.DefaultHairColor,
                Look = persistent.EquippedHairColor
            },
            EyeColor = new AppearanceComponentEntry
            {
                Shape = persistent.DefaultEyeColor,
                Look = persistent.EquippedEyeColor
            },
            SkinColor = new AppearanceComponentEntry
            {
                Shape = persistent.DefaultSkinColor,
                Look = persistent.EquippedSkinColor
            }
        });

        world.Set(entity, persistent.ToFatiguePointsComponent());

        world.Set(entity, new LeagueComponent
        {
            Id = 0,
            Name = string.Empty,
            Card = new LeagueComponentCard
            {
                Border = 0,
                Emblem = 0
            }
        });

        world.Set(entity, new PersonInfoComponent
        {
            Name = persistent.Name,
            Hero = persistent.Hero
        });

        world.Set(entity, new ShopPrivateComponent
        {
            Name = string.Empty,
            Type = 0
        });

        world.Set(entity, persistent.ToTitleComponent());

        world.Set(entity, persistent.ToCommunityComponent());

        world.Set(entity, accountOptions.ToPersonOptionComponent());

        // The six gestures kept ready, in a district and in a maze.
        world.Set(entity, new GestureComponent
        {
            Collection = persistent.GestureList.Length switch
            {
                < GesturesModuleDefines.MaxGestureCount => persistent.GestureList
                    .Concat(Enumerable.Repeat(0, GesturesModuleDefines.MaxGestureCount - persistent.GestureList.Length))
                    .ToArray(),

                > GesturesModuleDefines.MaxGestureCount => persistent.GestureList.Take(GesturesModuleDefines.MaxGestureCount).ToArray(),

                _ => persistent.GestureList
            }
        });

        world.Set(entity, new AccessComponent(Enum.IsDefined((AccessLevel)entry.AccessLevel) ? (AccessLevel)entry.AccessLevel : AccessLevel.None));

        // A password the account has must be given again on this server before a trade.
        world.Set(entity, new TradePasswordComponent
        {
            Password = entry.TradePassword,
            State = TradePasswordRules.StateAtEntry(!string.IsNullOrEmpty(entry.TradePassword), entry.TradePasswordFailed),
            FailedAttempts = entry.TradePasswordFailed
        });

        // Entering counts as the first answer to the keep-alive.
        world.Set(entity, KeepAliveComponent.Start(time.GetTimestamp()));
    }
}
