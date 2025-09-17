using System.Runtime.CompilerServices;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;

namespace OpenWorker.Hotspot.Handler.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class HotspotMessageAttribute : Attribute
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        SystemOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        LoginOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        CharacterOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        WorldOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        MoveOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        SkillOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        ChatOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        ItemOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        ShopOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        TradeOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        MazeOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        PartyOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        ItemUpgradeOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        DropOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        QuestOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        OptionOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        MonsterOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        ItemSetupOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        FriendOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        PostOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        SoulMetryOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        LeagueOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        GestureOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        DailyMissionOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        VaccumOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        MyRoomOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        HelperOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        InfiniteTowerOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        BoosterOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        EventOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        ExchangeOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        RankingOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        SocialItemOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        ForceOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        WorldModeOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        WeeklyMissionOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        ModeMazeOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        RestartOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        ToolOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        ChannelOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        ServerOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        ServerUserOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        ServerPartyOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        ServerFriendOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        ServerLeagueOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        MonitorOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        GmAgentOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        ServerForceOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        ServerWorldModeOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HotspotMessageAttribute(
        GroupOpcode group, 
        ServerModeMazeOpcode command, 
        HotspotMessageDirection direction = HotspotMessageDirection.None)
    {
        Opcode = new MessageOpcode(group, command);
        Direction = direction;
    }

    public MessageOpcode Opcode { get; }
    public HotspotMessageDirection Direction { get; }
}