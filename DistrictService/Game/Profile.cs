﻿using ow.Framework.Database.Characters;
using ow.Framework.Game.Types;

namespace DistrictService.Game
{
    public sealed class Profile
    {
        public ProfileStatusType Status { get; set; }
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