using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Enums;

namespace OpenWorker.Hotspot.Modules.Channels.Components;

[EntityComponent(EntityComponentService.District)]
public readonly record struct ChannelMemberComponent(short Index);