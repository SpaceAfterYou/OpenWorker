using Microsoft.Extensions.Configuration;
using ow.Framework.Game.Enums;
using System.Collections.Generic;

namespace ow.Framework.Game
{
    public sealed class Features : List<FeatureStatus>
    {
        public FeatureStatus this[Feature index] => this[(int)index];

        public FeatureStatus LoginBonus => this[Feature.LoginBonus];
        public FeatureStatus SecondaryPassword => this[Feature.SecondaryPassword];
        public FeatureStatus PremiumShop => this[Feature.PremiumShop];
        public FeatureStatus Option4 => this[Feature.Option4];
        public FeatureStatus Option5 => this[Feature.Option5];
        public FeatureStatus Option6 => this[Feature.Option6];
        public FeatureStatus Option7 => this[Feature.Option7];
        public FeatureStatus Option8 => this[Feature.Option8];
        public FeatureStatus Option9 => this[Feature.Option9];
        public FeatureStatus Option10 => this[Feature.Option10];
        public FeatureStatus Option11 => this[Feature.Option11];
        public FeatureStatus Option12 => this[Feature.Option12];
        public FeatureStatus Option13 => this[Feature.Option13];
        public FeatureStatus Option14 => this[Feature.Option14];

        public Features(IConfiguration config) : base(new FeatureStatus[]
        {
            config.GetValue<FeatureStatus>("Options:LoginBonus"),
            config.GetValue<FeatureStatus>("Options:SecondaryPassword"),
            config.GetValue<FeatureStatus>("Options:PremiumShop"),
            config.GetValue<FeatureStatus>("Options:Option4"),
            config.GetValue<FeatureStatus>("Options:Option5"),
            config.GetValue<FeatureStatus>("Options:Option6"),
            config.GetValue<FeatureStatus>("Options:Option7"),
            config.GetValue<FeatureStatus>("Options:Option8"),
            config.GetValue<FeatureStatus>("Options:Option9"),
            config.GetValue<FeatureStatus>("Options:Option10"),
            config.GetValue<FeatureStatus>("Options:Option11"),
            config.GetValue<FeatureStatus>("Options:Option12"),
            config.GetValue<FeatureStatus>("Options:Option13"),
            config.GetValue<FeatureStatus>("Options:Option14"),
        })
        {
        }
    }
}