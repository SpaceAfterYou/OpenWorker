using Core.DatabaseSystem.Characters;

namespace GateService.Systems.GameSystem
{
    public sealed class Hair
    {
        public ushort Style { get; init; }
        public ushort Color { get; init; }

        public Hair(HairModel model)
        {
            Style = model.Style;
            Color = model.Color;
        }
    }
}