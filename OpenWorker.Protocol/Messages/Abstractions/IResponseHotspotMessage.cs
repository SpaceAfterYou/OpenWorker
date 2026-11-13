using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Messages.Abstractions;

/// <summary>
/// A message for the client. It writes itself through <see cref="PacketWriter"/>, straight into its frame.
/// </summary>
public interface IResponseHotspotMessage : IHotspotMessage, IWireWritable;
