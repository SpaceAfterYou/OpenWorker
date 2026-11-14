using System.Numerics;
using OpenWorker.Batch.Actions;

namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// Where the traps of a pattern go, as the clients work it out. A scattered trap takes its distance and its turn from a table of
/// numbers every client builds the same way, read in turn from the key the start of the skill told them, so the server has to
/// read the same numbers to hit where the clients draw.
/// </summary>
public static class TrapLayout
{
    /// <summary>
    /// How many numbers the table holds; a key wraps round to zero at the end.
    /// </summary>
    public const int Keys = 1_000_000;

    private const float DegreesToRadians = MathF.PI / 180f;

    private static readonly Lazy<ushort[]> Table = new(Build);

    /// <summary>
    /// The number of the table for a key, from 0 to 9999.
    /// </summary>
    public static int Number(int key)
    {
        return Table.Value[key is >= 0 and < Keys ? key : 0];
    }

    /// <summary>
    /// Where the next scattered trap goes, around the place of its maker turned the way it looks. It takes three keys in turn.
    /// </summary>
    public static Vector3 Scatter(TrapInfo trap, Vector3 maker, float yaw, ref int key)
    {
        var distance = Number(Take(ref key));
        var turn = Number(Take(ref key));
        var side = Number(Take(ref key));

        var span = (int)(trap.ScatterRadius - trap.ScatterRadiusMin);
        var length = (int)trap.ScatterRadiusMin + (span >= 0 ? distance % (span + 1) : 0);
        var angle = turn % Math.Max(1, (int)(trap.ScatterAngle + 1f));

        if (side % 2 == 0)
        {
            angle = -angle;
        }

        var middle = maker + Turn(trap.Offset, yaw);

        return middle + Turn(new Vector3(0f, -length, 0f), yaw + angle);
    }

    /// <summary>
    /// Where a trap of a row goes: the first at the start, each next one a step further, turned the way the maker looks.
    /// </summary>
    public static Vector3 InRow(TrapInfo trap, Vector3 start, float yaw, int index)
    {
        return start + Turn(trap.Offset, yaw) * index;
    }

    /// <summary>
    /// Turns a place about the vertical by a yaw in degrees: at zero, -Y stays ahead.
    /// </summary>
    public static Vector3 Turn(Vector3 offset, float yaw)
    {
        var (sin, cos) = MathF.SinCos(yaw * DegreesToRadians);

        return new Vector3(offset.X * cos - offset.Y * sin, offset.X * sin + offset.Y * cos, offset.Z);
    }

    private static int Take(ref int key)
    {
        if (key is < 0 or >= Keys)
        {
            key = 0;
        }

        return key++;
    }

    /// <summary>
    /// The table: a combined linear generator with a shuffle, started from the first number the C library gives after a seed of
    /// zero, each value scaled to ten thousand.
    /// </summary>
    private static ushort[] Build()
    {
        const int modulus = 2147483563;
        const int otherModulus = 2147483399;
        const int shuffle = 32;
        const double scale = 1.0 / modulus;
        const double highest = 1.0 - 1.2e-7;

        var table = new ushort[Keys];
        var slots = new int[shuffle];
        var first = -FirstLibraryNumber();
        var second = 123456789;
        var last = 0;

        for (var i = 0; i < Keys; i++)
        {
            if (first <= 0)
            {
                first = Math.Max(-first, 1);
                second = first;

                for (var j = shuffle + 7; j >= 0; j--)
                {
                    first = Step(first, 40014, 53668, modulus);

                    if (j < shuffle)
                    {
                        slots[j] = first;
                    }
                }

                last = slots[0];
            }

            first = Step(first, 40014, 53668, modulus);
            second = Step(second, 40692, 52774, otherModulus);

            var slot = last / (1 + (modulus - 1) / shuffle);

            last = unchecked(slots[slot] - second);
            slots[slot] = first;

            if (last < 1)
            {
                last += modulus - 1;
            }

            table[i] = (ushort)(int)(Math.Min(last * scale, highest) * 10000.0);
        }

        return table;
    }

    private static int Step(int value, int factor, int quotient, int modulus)
    {
        var next = unchecked((factor * value) - (modulus * (value / quotient)));

        return next < 0 ? next + modulus : next;
    }

    /// <summary>
    /// The first number above zero the C library of the clients gives after a seed of zero.
    /// </summary>
    private static int FirstLibraryNumber()
    {
        var state = 0u;
        int number;

        do
        {
            state = unchecked((state * 214013u) + 2531011u);
            number = (int)((state >> 16) & 0x7FFF);
        }
        while (number == 0);

        return number;
    }
}
