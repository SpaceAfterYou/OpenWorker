using ow.Framework.Database.Characters;

namespace ow.Framework.Game.Character
{
    public sealed class HairCharacter
    {
        public ushort Style { get; set; }
        public ushort Color { get; set; }

        public HairCharacter(HairModel model) => (Style, Color) = (model.Style, model.Color);
    }
}