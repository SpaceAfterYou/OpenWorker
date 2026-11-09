using Arch.Core;
using Microsoft.Extensions.Options;
using OpenWorker.Domain.Components;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Channels.Enums;
using OpenWorker.Hotspot.Modules.Channels.Responses;
using OpenWorker.Hotspot.Modules.Channels.Types;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using Redis.OM;
using Redis.OM.Searching;

namespace OpenWorker.Channel;

/// <summary>
/// The channels a player is offered and the change of channel. The list holds the channels of this server only, so a
/// change goes to a channel of this process or to none: a channel the server does not run is not found.
/// </summary>
public sealed class ServerChannels(
    IOptions<InstanceOptions> instance,
    IOptions<GateOptions> gate,
    IRedisCollection<ChannelCache> channelCache,
    ServiceChannels channels,
    World world
) {
    /// <summary>
    /// The client's line for a channel it cannot find (the same as a failed move between worlds).
    /// </summary>
    public const ushort NotFoundCode = 50004;

    /// <summary>
    /// The client's line "the channel cannot be entered": a full channel, and the channel the player is already in.
    /// </summary>
    public const ushort CannotEnterCode = 51008;

    private Guid Owner { get; } = instance.Value.Identifier;
    private short Location { get; } = instance.Value.Location;
    private string Host { get; } = instance.Value.PublicHost;
    private short Port { get; } = (short)(instance.Value.PublicPort ?? 0);
    private short Gate { get; } = gate.Value.Id;

    public async ValueTask SendListAsync(Entity entity, CancellationToken cancellationToken)
    {
        var session = world.Get<ServerSessionComponent>(entity);

        var all = await channelCache
            .Where(e => e.Owner == Owner && e.Location == Location)
            .ToArrayAsync(cancellationToken);

        var values = all
            .Select(e => new ChannelValue(e.Identifier, e.Workload))
            .ToArray();

        session.Send(new ChannelInfoResponse
        {
            Location = Location,
            Values = values
        });
    }

    /// <summary>
    /// Moves the player to another channel of this server and answers the client: the map of the new channel on success,
    /// which the client takes as its own and then asks what is around it, or the line of the refusal.
    /// </summary>
    public ChannelSwitchResult Switch(Entity entity, short identifier)
    {
        var result = channels.TrySwitch(entity, identifier);
        var session = world.Get<ServerSessionComponent>(entity);

        if (result is not ChannelSwitchResult.Ok)
        {
            Refuse(entity, result is ChannelSwitchResult.NotFound ? NotFoundCode : CannotEnterCode);

            return result;
        }

        session.Send(new ChannelChangeResponse { Value = Answer(entity, identifier) });

        return result;
    }

    /// <summary>
    /// The error answer of the change, which the client shows as the line of the code.
    /// </summary>
    public void Refuse(Entity entity, ushort code)
    {
        world.Get<ServerSessionComponent>(entity)
            .Send(new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Channel, ChannelOpcode.Change), code));
    }

    private EnterMapResultValue Answer(Entity entity, short identifier)
    {
        var map = new MapValue
        {
            Channel = identifier,
            Location = Location,
            Server = Gate
        };

        var place = world.Get<WorldComponent>(entity);

        return new EnterMapResultValue
        {
            Zone = new ZoneValue
            {
                Actor = world.Get<ActorComponent>(entity).Identifier,
                Account = world.Get<ClaimsComponent>(entity),
                Map = map,
                Address = string.IsNullOrEmpty(Host) ? "0.0.0.0" : Host,
                Port = Port,
                Jump = place.Jump,
                World = new WorldValue
                {
                    Location = Location,
                    Position = place.Position,
                    Rotation = place.Rotation,
                    Map = map
                },
                Type = EnterMapType.EnterDistrict
            },
            ChangeServer = false,
            ChangeType = ChangeServerType.EnterDistrict
        };
    }
}
