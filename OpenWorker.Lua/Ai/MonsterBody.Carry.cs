using System.Numerics;
using OpenWorker.Hotspot.Modules.Movement.Responses;

namespace OpenWorker.Lua.Ai;

/// <summary>
/// A monster that a player carries away with a skill: it goes where the player takes it and does nothing of its own until it is
/// let go. The clients move it themselves from the word that it is carried; the server keeps its place in step.
/// </summary>
internal partial class MonsterBody
{
    private bool _carried;

    /// <summary>
    /// A player carries the monster.
    /// </summary>
    public bool IsCarried => _carried;

    /// <summary>
    /// The monster is taken: whatever it was doing is over, and it is told to stand where it is.
    /// </summary>
    public void BeginCarried()
    {
        _carried = true;
        _attackEnd = 0;
        _pending.Clear();
        _flights.Clear();
        _destination = null;

        ReleaseGrab(true);
        SendStop();
    }

    /// <summary>
    /// The monster has been carried to a place.
    /// </summary>
    public void CarriedTo(Vector3 position)
    {
        Place(position, Yaw);
    }

    /// <summary>
    /// The player lets the monster go where it stands.
    /// </summary>
    public void EndCarried()
    {
        if (_carried)
        {
            _carried = false;

            SendStop();
            Broadcast(new MovementAttacedEndBtResponse { Actor = Actor });
        }
    }
}
