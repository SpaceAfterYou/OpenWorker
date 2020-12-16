using ow.Framework.Database.Characters;
using ow.Framework.Game.Types;

namespace ow.Framework.Game.Entities
{
    public sealed class ProfileEntity
    {
        public ProfileStatus Status { get; set; }
        public string About { get; set; }
        public string Note { get; set; }

        public ProfileEntity(ProfileModel model)
        {
            Status = model.Status;
            About = model.About;
            Note = model.Note;
        }
    }
}