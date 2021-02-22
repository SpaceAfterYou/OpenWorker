using ow.Framework.Database.Characters;
using SoulCore.Types;

namespace ow.Service.District.Game
{
    public sealed class Profile
    {
        internal static readonly Profile Empty = new();

        public ProfileStatus Status { get; set; } = ProfileStatus.Rookie;
        public string About { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;

        public Profile(ProfileModel model)
        {
            Status = model.Status;
            About = model.About;
            Note = model.Note;
        }

        private Profile()
        {
        }
    }
}