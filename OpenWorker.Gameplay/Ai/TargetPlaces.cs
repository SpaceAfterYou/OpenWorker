using System.Numerics;

namespace OpenWorker.Gameplay.Ai;

/// <summary>
/// The places around each target that the monsters fighting it share out: twelve, a twelfth of a turn apart, so that the
/// monsters that come at one target spread round it instead of standing on one spot.
/// </summary>
public sealed class TargetPlaces
{
    public const int Count = 12;

    public const int None = -1;

    private const int Width = 360 / Count;

    private readonly Dictionary<int, byte[]> _taken = [];

    /// <summary>
    /// The place a monster takes around a target, by the side it stands on: the place of that side when it is free, else a
    /// free one of the two on either side of it, else the one of them the fewest monsters hold.
    /// </summary>
    /// <param name="bearing">The way from the target to the monster, in degrees.</param>
    public int Choose(int target, float bearing)
    {
        var taken = TakenAround(target);
        var yaw = (int)bearing;

        yaw += yaw < 0 ? 360 : 0;

        var place = Math.Clamp(yaw / Width, 0, 2 * Count - 1) % Count;

        if (taken[place] == 0)
        {
            return place;
        }

        var next = (place + 1) % Count;
        var previous = (place + Count - 1) % Count;

        if (taken[previous] == 0)
        {
            return previous;
        }

        if (taken[next] == 0)
        {
            return next;
        }

        var farNext = (place + 2) % Count;
        var farPrevious = (place + Count - 2) % Count;

        if (taken[farPrevious] == 0)
        {
            return farPrevious;
        }

        if (taken[farNext] == 0)
        {
            return farNext;
        }

        if (taken[place] <= taken[previous] && taken[place] <= taken[next])
        {
            return place;
        }

        if (taken[place] == taken[previous] + 1)
        {
            return previous;
        }

        if (taken[place] == taken[next] + 1)
        {
            return next;
        }

        if (taken[place] <= taken[farPrevious] && taken[place] <= taken[farNext])
        {
            return place;
        }

        if (taken[place] >= taken[farPrevious] + 1)
        {
            return farPrevious;
        }

        return taken[place] < taken[farNext] + 1 ? place : farNext;
    }

    /// <summary>
    /// The place a monster takes to stand before a target, within angles from where the target looks: the first free one of
    /// those angles, on its right or its left, else the one the fewest monsters hold.
    /// </summary>
    public int ChooseByAngle(int target, float angleMin, float angleMax)
    {
        var taken = TakenAround(target);
        var low = Math.Min(angleMin, 180f);
        var high = Math.Min(angleMax, 180f);
        var begin = Math.Max(0, (int)(low / Width));
        var end = (int)(high / Width);

        if ((int)high % Width > 0 && high > Width * begin)
        {
            end = Math.Min(end + 1, Count / 2 - 1);
        }

        var best = begin;
        var fewest = int.MaxValue;

        for (var next = begin; next < end; next++)
        {
            foreach (var place in (ReadOnlySpan<int>)[next, Count - next - 1])
            {
                if (taken[place] == 0)
                {
                    return place;
                }

                if (taken[place] < fewest)
                {
                    fewest = taken[place];
                    best = place;
                }
            }
        }

        return best;
    }

    /// <summary>
    /// The angle of a place from where its target looks, in degrees.
    /// </summary>
    public static float Angle(int place) => Width * (place + 1);

    public void Take(int target, int place)
    {
        if (place is >= 0 and < Count)
        {
            var taken = TakenAround(target);

            taken[place] = (byte)Math.Min(taken[place] + 1, byte.MaxValue);
        }
    }

    public void Leave(int target, int place)
    {
        if (place is >= 0 and < Count && _taken.TryGetValue(target, out var taken) && taken[place] > 0)
        {
            taken[place]--;
        }
    }

    /// <summary>
    /// The target is gone: nobody holds a place around it any more.
    /// </summary>
    public void Forget(int target)
    {
        _taken.Remove(target);
    }

    /// <summary>
    /// How many monsters hold a place around a target.
    /// </summary>
    public int Holders(int target, int place)
    {
        return place is >= 0 and < Count && _taken.TryGetValue(target, out var taken) ? taken[place] : 0;
    }

    /// <summary>
    /// Where a place lies from its target at a distance, on the ground.
    /// </summary>
    public static Vector2 Offset(int place, float distance)
    {
        return AiMath.Direction(Width * (place + 1)) * distance;
    }

    private byte[] TakenAround(int target)
    {
        if (!_taken.TryGetValue(target, out var taken))
        {
            taken = new byte[Count];
            _taken.Add(target, taken);
        }

        return taken;
    }
}
