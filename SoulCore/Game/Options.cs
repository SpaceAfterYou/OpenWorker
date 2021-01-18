using Microsoft.Extensions.Configuration;
using SoulCore.Game.Enums;
using System.Collections.Generic;

namespace SoulCore.Game
{
    public sealed class Options : List<OptionStatus>
    {
        public OptionStatus this[Option index] => this[(int)index];

        public OptionStatus Attendance => this[Option.Attendance];
        public OptionStatus SecondPassword => this[Option.SecondPassword];
        public OptionStatus PvpDistrict => this[Option.PvpDistrict];
        public OptionStatus Ranking => this[Option.Ranking];
        public OptionStatus CashShop => this[Option.CashShop];
        public OptionStatus D6Mode => this[Option.D6Mode];
        public OptionStatus BroachEvent => this[Option.BroachEvent];
        public OptionStatus OverIndulgence => this[Option.OverIndulgence];
        public OptionStatus SoulWeeklyMission => this[Option.SoulWeeklyMission];
        public OptionStatus NetCafe => this[Option.NetCafe];
        public OptionStatus SoulEvent => this[Option.SoulEvent];
        public OptionStatus ItemExchange => this[Option.ItemExchange];
        public OptionStatus WaitSystem => this[Option.WaitSystem];
        public OptionStatus OperationMaze => this[Option.OperationMaze];

        public Options(IConfiguration config) : base(new OptionStatus[]
        {
            config.GetValue<OptionStatus>("Options:Attendance"),
            config.GetValue<OptionStatus>("Options:SecondPassword"),
            config.GetValue<OptionStatus>("Options:PvpDistrict"),
            config.GetValue<OptionStatus>("Options:Ranking"),
            config.GetValue<OptionStatus>("Options:CashShop"),
            config.GetValue<OptionStatus>("Options:D6Mode"),
            config.GetValue<OptionStatus>("Options:BroachEvent"),
            config.GetValue<OptionStatus>("Options:OverIndulgence"),
            config.GetValue<OptionStatus>("Options:SoulWeeklyMission"),
            config.GetValue<OptionStatus>("Options:NetCafe"),
            config.GetValue<OptionStatus>("Options:SoulEvent"),
            config.GetValue<OptionStatus>("Options:ItemExchange"),
            config.GetValue<OptionStatus>("Options:WaitSystem"),
            config.GetValue<OptionStatus>("Options:OperationMaze"),
        })
        {
        }
    }
}
