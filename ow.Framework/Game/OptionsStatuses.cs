using Microsoft.Extensions.Configuration;
using ow.Framework.Game.Enums;
using System.Collections.Generic;

namespace ow.Framework.Game
{
    public sealed class OptionsStatuses : List<OptionStatus>
    {
        public OptionStatus this[Option index] => this[(int)index];

        public OptionStatus LoginBonus => this[Option.LoginBonus];
        public OptionStatus SecondaryPassword => this[Option.SecondaryPassword];
        public OptionStatus PremiumShop => this[Option.PremiumShop];
        public OptionStatus Option4 => this[Option.Option4];
        public OptionStatus Option5 => this[Option.Option5];
        public OptionStatus Option6 => this[Option.Option6];
        public OptionStatus Option7 => this[Option.Option7];
        public OptionStatus Option8 => this[Option.Option8];
        public OptionStatus Option9 => this[Option.Option9];
        public OptionStatus Option10 => this[Option.Option10];
        public OptionStatus Option11 => this[Option.Option11];
        public OptionStatus Option12 => this[Option.Option12];
        public OptionStatus Option13 => this[Option.Option13];
        public OptionStatus Option14 => this[Option.Option14];

        public OptionsStatuses(IConfiguration config) : base(new List<OptionStatus>()
        {
            config.GetValue<OptionStatus>("Options:LoginBonus"),
            config.GetValue<OptionStatus>("Options:SecondaryPassword"),
            config.GetValue<OptionStatus>("Options:PremiumShop"),
            config.GetValue<OptionStatus>("Options:Option4"),
            config.GetValue<OptionStatus>("Options:Option5"),
            config.GetValue<OptionStatus>("Options:Option6"),
            config.GetValue<OptionStatus>("Options:Option7"),
            config.GetValue<OptionStatus>("Options:Option8"),
            config.GetValue<OptionStatus>("Options:Option9"),
            config.GetValue<OptionStatus>("Options:Option10"),
            config.GetValue<OptionStatus>("Options:Option11"),
            config.GetValue<OptionStatus>("Options:Option12"),
            config.GetValue<OptionStatus>("Options:Option13"),
            config.GetValue<OptionStatus>("Options:Option14"),
        })
        {
        }
    }
}