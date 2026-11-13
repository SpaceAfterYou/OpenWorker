using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Buffs;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Server.Common;

namespace OpenWorker.DistrictServer.Services;

/// <summary>
/// The buffs of the persons of a district: the ones that may be there (a row of the buff table names the district or says
/// anywhere) and that a person carried in or that its clothing gives. A buff is told to the person and to those who see it, ticks
/// and runs out on the clock, and goes with the person to the next server.
/// </summary>
public sealed class DistrictBuffs(World world, BuffManager manager, IPlayerAudience audience, PlayerStatsService stats, SimulationClock clock)
    : ISimulation, ISessionCloseHandler
{
    /// <summary>How often the clothing of a person is matched against its buffs.</summary>
    private const float SourceInterval = 1f;

    private readonly Dictionary<Entity, Carried> _persons = [];
    private readonly List<BuffChange> _changes = [];
    private bool _running;

    private sealed class Carried
    {
        public SourceBuffs Sources { get; } = new();

        public float Clock { get; set; }
    }

    /// <summary>
    /// A person has entered the district: it gets the buffs it carried in, and those of its clothing.
    /// </summary>
    public void Enter(Entity player)
    {
        if (!world.IsAlive(player) || !world.Has<ActorComponent>(player))
        {
            return;
        }

        if (!_running)
        {
            _running = true;
            clock.Add(this);
        }

        var book = new BuffBook();
        var state = new Carried();

        if (world.Has<BuffBookComponent>(player))
        {
            world.Set(player, new BuffBookComponent { Book = book });
        }
        else
        {
            world.Add(player, new BuffBookComponent { Book = book });
        }

        _persons[player] = state;

        foreach (var saved in BuffMemory.Saved(world, player))
        {
            BuffRules.Restore(book, manager.Catalog, saved.Buff, saved.Time, saved.Count, Carrier(player), _changes);
        }

        Reconcile(player, book, state);
        Tell(player, book);
    }

    /// <summary>
    /// Gives a person a buff for sure, as a potion or a command does, when its row lets the buff be in a district.
    /// </summary>
    public bool Give(Entity player, int buff, int owner = 0)
    {
        if (!_persons.ContainsKey(player) || BuffBook.Of(world, player) is not { } book || !manager.Catalog.TryGet(buff, out var definition))
        {
            return false;
        }

        var given = BuffRules.Apply(book, manager.Catalog, definition, owner, Carrier(player), _changes);

        Tell(player, book);

        return given;
    }

    public void Advance(TimeSpan elapsed)
    {
        var seconds = (float)elapsed.TotalSeconds;

        foreach (var (player, state) in _persons.ToArray())
        {
            if (!world.IsAlive(player) || BuffBook.Of(world, player) is not { } book)
            {
                _persons.Remove(player);

                continue;
            }

            state.Clock += seconds;

            if (state.Clock >= SourceInterval)
            {
                state.Clock = 0f;

                Reconcile(player, book, state);
            }

            if (!book.IsEmpty)
            {
                BuffRules.Step(book, manager.Catalog, seconds, Carrier(player), _changes);
            }

            if (_changes.Count > 0)
            {
                Tell(player, book);
            }
        }
    }

    public ValueTask OnSessionClosedAsync(Entity player)
    {
        if (_persons.Remove(player, out var state) && BuffBook.Of(world, player) is { } book)
        {
            BuffMemory.Remember(world, player, book, state.Sources.Given);
        }

        return ValueTask.CompletedTask;
    }

    private void Reconcile(Entity player, BuffBook book, Carried state)
    {
        var learned = world.TryGet<SkillLearnedComponent>(player, out var skills) ? skills.Values : null;
        var inventory = world.TryGet<InventoryComponent>(player, out var bag) ? bag.Value : null;

        BuffSources.Reconcile(book, manager.Catalog, manager.Passives, learned, inventory, state.Sources, Carrier(player), _changes);
    }

    private BuffCarrier Carrier(Entity player)
    {
        var health = world.TryGet<AbilityComponent>(player, out var ability) ? ability.Health : default;

        return new BuffCarrier((int)world.Get<ActorComponent>(player), health.Current, health.Max, false, BuffDefinition.District);
    }

    /// <summary>
    /// Tells the person and those who see it what changed, and the person its stats.
    /// </summary>
    private void Tell(Entity player, BuffBook book)
    {
        if (_changes.Count == 0)
        {
            return;
        }

        var actor = world.Get<ActorComponent>(player);
        var recount = false;

        foreach (var change in _changes)
        {
            switch (change.Kind)
            {
                case BuffChangeKind.Updated:
                    Show(player, new SkillUpdateBuffResponse { Target = actor, Buff = (short)change.Buff, Time = Math.Max(0f, change.Time), Count = change.Count, Owner = change.Owner });
                    break;

                case BuffChangeKind.Changed:
                    Show(player, new SkillBuffChangeBroadcastResponse { Target = actor, Buff = (short)change.Buff, NewBuff = (short)change.Other, Time = Math.Max(0f, change.Time), Count = change.Count, Owner = change.Owner });
                    break;

                case BuffChangeKind.Deleted:
                    Show(player, new SkillBuffDeleteBroadcastResponse { Actor = actor, Buff = (short)change.Buff, ExecuteOutSkill = change.CallOut });
                    break;

                case BuffChangeKind.Recounted:
                    recount = true;
                    break;
            }
        }

        _changes.Clear();

        if (recount)
        {
            stats.Send(player);
        }
    }

    private void Show<T>(Entity player, in T message) where T : OpenWorker.Hotspot.Messages.Abstractions.IResponseHotspotMessage
    {
        if (world.TryGet<ServerSessionComponent>(player, out var session))
        {
            session.Send(in message);
        }

        audience.NearbyToOthers(player, in message);
    }
}

public static class DistrictBuffsServiceCollectionExtensions
{
    /// <summary>
    /// The buffs of a district: its persons carry them, they tick on a clock of the district and go with the person when it leaves.
    /// </summary>
    public static IServiceCollection AddDistrictBuffs(this IServiceCollection services)
    {
        services.AddSingleton<BuffManager>();
        services.AddSingleton<SimulationClock>();
        services.AddHostedService(provider => provider.GetRequiredService<SimulationClock>());
        services.AddSingleton<DistrictBuffs>();
        services.Replace(ServiceDescriptor.Singleton<IItemField, DistrictItemBuffs>());
        // Before the handler that writes the person: what the person carries is written down first.
        services.Insert(0, ServiceDescriptor.Singleton<ISessionCloseHandler>(provider => provider.GetRequiredService<DistrictBuffs>()));

        return services;
    }
}
