using System.IO;

namespace OpenWorker.Hotspot.Modules.Skill.Responses;

/// <summary>Серверный суффикс ответов скилов: <c>PS_TICKCOUNT_INFO</c> (не путать с запросным <c>PS_ROULETTE_INFO</c> / <see cref="Requests.SkillRequestTick"/>).</summary>
public readonly struct SkillTickCountInfo
{
    public int TickNum { get; init; }

    public byte Type { get; init; }

    public ulong ReqTickCount { get; init; }

    public ulong ResTickCount { get; init; }

    public ulong GetTickCount { get; init; }

    public ulong ReqTickCount64 { get; init; }

    public ulong ResTickCount64 { get; init; }

    public ulong GetTickCount64 { get; init; }

    public int Fps { get; init; }

    public SkillTickCountInfo(BinaryReader reader)
    {
        TickNum = reader.ReadInt32();
        Type = reader.ReadByte();
        ReqTickCount = reader.ReadUInt64();
        ResTickCount = reader.ReadUInt64();
        GetTickCount = reader.ReadUInt64();
        ReqTickCount64 = reader.ReadUInt64();
        ResTickCount64 = reader.ReadUInt64();
        GetTickCount64 = reader.ReadUInt64();
        Fps = reader.ReadInt32();
    }

    public static SkillTickCountInfo Empty => default;

    public void Write(BinaryWriter writer)
    {
        writer.Write(TickNum);
        writer.Write(Type);
        writer.Write(ReqTickCount);
        writer.Write(ResTickCount);
        writer.Write(GetTickCount);
        writer.Write(ReqTickCount64);
        writer.Write(ResTickCount64);
        writer.Write(GetTickCount64);
        writer.Write(Fps);
    }
}
