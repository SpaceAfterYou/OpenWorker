namespace OpenWorker.Hotspot;

public static class SoulMetry
{
    public static short ChangeEpisode(short value, short index, bool flag)
    {
        var mask = (short)(1 << index);
        return (short)(flag ? value | mask : value & ~mask);
    }
}