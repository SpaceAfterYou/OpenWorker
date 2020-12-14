using ow.Framework.Game.Types;

namespace ow.Framework.Database.Characters
{
    public sealed class ProfileModel
    {
        public ProfileStatus Status { get; set; } = ProfileStatus.Rookie;
        public string About { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }
}