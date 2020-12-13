using ow.Framework.Database.Characters;
using ow.Framework.Game;

namespace ow.Service.Gate.Game
{
    public sealed class Hair : ICharacterHair
    {
        public ushort Style { get; }
        public ushort Color { get; }

        public Hair(HairModel model) => (Style, Color) = (model.Style, model.Color);
    }
}