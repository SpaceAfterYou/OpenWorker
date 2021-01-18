using SoulCore.Database.Characters;
using SoulCore.Game.Types;

namespace ow.Service.District.Game
{
    public sealed class Profile
    {
        public ProfileStatus Status { get; set; }
        public string About { get; set; }
        public string Note { get; set; }

        public Profile(ProfileModel model)
        {
            Status = model.Status;
            About = model.About;
            Note = model.Note;
        }
    }
}
