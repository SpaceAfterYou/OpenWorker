using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Handler.Abstractions;

public interface IHotspotHandler;

public interface IHotspotHandler<in TMessage> : IHotspotHandler where TMessage : IRequestHotspotMessage
{
    ValueTask OnHandleAsync(ServiceHandleContext context, TMessage request);
}

// https://youtu.be/OUD62LIbg20?list=RDVfoCTeznCAk