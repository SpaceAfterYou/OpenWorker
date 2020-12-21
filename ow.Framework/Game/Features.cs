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
            config.GetValue<FeatureStatus>("Features:LoginBonus"),
            config.GetValue<FeatureStatus>("Features:SecondaryPassword"),
            config.GetValue<FeatureStatus>("Features:PremiumShop"),
            config.GetValue<FeatureStatus>("Features:Feature4"),
            config.GetValue<FeatureStatus>("Features:Feature5"),
            config.GetValue<FeatureStatus>("Features:Feature6"),
            config.GetValue<FeatureStatus>("Features:Feature7"),
            config.GetValue<FeatureStatus>("Features:Feature8"),
            config.GetValue<FeatureStatus>("Features:Feature9"),
            config.GetValue<FeatureStatus>("Features:Feature10"),
            config.GetValue<FeatureStatus>("Features:Feature11"),
            config.GetValue<FeatureStatus>("Features:Feature12"),
            config.GetValue<FeatureStatus>("Features:Feature13"),
            config.GetValue<FeatureStatus>("Features:Feature14"),
        })
        {
        }
    }
}
