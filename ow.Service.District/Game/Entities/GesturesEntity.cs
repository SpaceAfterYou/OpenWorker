using ow.Framework.Database.Characters;
using System.Collections.Generic;

namespace ow.Service.District.Game.Entities
{
    internal sealed class GesturesEntity : List<uint>
    {
        internal GesturesEntity(CharacterModel model) : base(model.Gestures)
        {
        }
    }
}