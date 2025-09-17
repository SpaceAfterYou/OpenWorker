namespace OpenWorker.Hotspot.SoulWorker.Network.DataTypes;

// [StructLayout(LayoutKind.Explicit, Size = sizeof(long))]
// public readonly struct MapEntity(BinaryReader reader)
// {
//     [field: FieldOffset(CHANNEL_OFFSET)] public byte Channel { get; init; }
//
//     [field: FieldOffset(MAP_OFFSET)] public byte Map { get; init; }
//
//     [field: FieldOffset(SERVER_OFFSET)] public byte Server { get; init; }
//
//     [field: FieldOffset(0)] internal long Value { get; } = reader.ReadInt64();
//
//     private const int CHANNEL_OFFSET = 0;
//     private const int MAP_OFFSET = 24 + CHANNEL_OFFSET + sizeof(short);
//     private const int SERVER_OFFSET = MAP_OFFSET + sizeof(short);
// }