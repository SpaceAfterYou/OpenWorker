using Arch.Core;
using OpenWorker.Gameplay.Modules.Persons.Extensions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Login.Extensions;
using OpenWorker.Gameplay.Modules.League.Components;
using OpenWorker.Gameplay.Modules.Maze.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;

namespace OpenWorker.MazeServer.Services;

public sealed class PersonRegistry(World world)
{
    internal void PullPerson(Entity entity, PersonPersistent persistent)
    {
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

        world.Set(entity, new FatiguePointsComponent
        {
            Common = 100,
            Bonus = 100
        });

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

        world.Set(entity, persistent.ToPersonOptionComponent());
    }
}
