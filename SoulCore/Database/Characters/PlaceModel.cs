namespace SoulCore.Database.Characters
{
    public sealed class PlaceModel
    {
        public Vector3Model Position { get; set; } = default!;
        public float Rotation { get; set; }
        public ushort Location { get; set; }
    }
}
