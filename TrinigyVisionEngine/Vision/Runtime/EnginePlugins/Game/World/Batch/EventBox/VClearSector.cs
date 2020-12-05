namespace TrinigyVisionEngine.Vision.Runtime.EnginePlugins.Game.World.Batch.EventBox
{
    public sealed class VClearSector
    {
        /// <summary>
        ///
        /// </summary>
        public int Id { get; }

        /// <summary>
        ///
        /// </summary>
        public float Chance { get; }

        internal VClearSector(int id, float chance)
            => (Id, Chance) = (id, chance);
    }
}
