using Microsoft.Extensions.Configuration;

namespace LoginService.Systems.GameSystem
{
    public sealed class Options
    {
        public Option LoginBonus { get; }
        public Option SecondaryPassword { get; }
        public Option PremiumShop { get; }
        public Option Option4 { get; }
        public Option Option5 { get; }
        public Option Option6 { get; }
        public Option Option7 { get; }
        public Option Option8 { get; }
        public Option Option9 { get; }
        public Option Option10 { get; }
        public Option Option11 { get; }
        public Option Option12 { get; }
        public Option Option13 { get; }
        public Option Option14 { get; }

        public Options(IConfiguration config)
        {
            LoginBonus = config.GetValue<Option>("Options:LoginBonus");
            SecondaryPassword = config.GetValue<Option>("Options:SecondaryPassword");
            PremiumShop = config.GetValue<Option>("Options:PremiumShop");
            Option4 = config.GetValue<Option>("Options:Option4");
            Option5 = config.GetValue<Option>("Options:Option5");
            Option6 = config.GetValue<Option>("Options:Option6");
            Option7 = config.GetValue<Option>("Options:Option7");
            Option8 = config.GetValue<Option>("Options:Option8");
            Option9 = config.GetValue<Option>("Options:Option9");
            Option10 = config.GetValue<Option>("Options:Option10");
            Option11 = config.GetValue<Option>("Options:Option11");
            Option12 = config.GetValue<Option>("Options:Option12");
            Option13 = config.GetValue<Option>("Options:Option13");
            Option14 = config.GetValue<Option>("Options:Option14");
        }
    }
}