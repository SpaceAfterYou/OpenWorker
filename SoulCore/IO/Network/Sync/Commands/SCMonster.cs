namespace SoulCore.IO.Network.Sync.Commands
{
    public enum SCMonster : byte
    {
        Die = 0x11,
        UpdateStatList = 0x12,
        Delete = 0x13,
        ReserveMotion = 0x14,
        TargetChange = 0x21,
        TargetChangeBt = 0x22,
        InvisibleBt = 0x32,
        ChangeMotionBt = 0x33,
        EscapeDamageBt = 0x34,
        DefensiveWeaponStartReq = 0x41,
        DefensiveWeaponStartBt = 0x42,
        DefensiveWeaponEndReq = 0x43,
        DefensiveWeaponEndBt = 0x44,
        DefensiveWeaponAttackReq = 0x45,
        ControlMonsterBt = 0x51,
        ControlMonsterAttackReq = 0x52,
        SuperArmorGage = 0x53,
        PartsHp = 0x54,
        WrongPos = 0x55,
    }
}
