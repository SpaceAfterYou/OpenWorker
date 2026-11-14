namespace OpenWorker.Lua.Ai;

/// <summary>
/// The combos of a monster: as a skill starts, the table of the server may give it the next skill to go on with. The monster goes
/// on when the end animation of the skill may first be cut short, where it stands and the way it looks, and the next skill rolls
/// for its own next one. A skill whose end animation cannot be cut makes no combo, and whatever ends the skill ends the combo.
/// </summary>
internal partial class MonsterBody
{
    private int _comboSkill;
    private double _comboAt = double.PositiveInfinity;

    private void RollCombo(int skill, float? at, double now)
    {
        DropCombo();

        var next = _manager.Services?.Chains?.Roll(skill, _manager.Random) ?? 0;

        if (next != 0 && at is { } moment)
        {
            _comboSkill = next;
            _comboAt = now + moment;
        }
    }

    private void DropCombo()
    {
        _comboSkill = 0;
        _comboAt = double.PositiveInfinity;
    }

    private void GoOnWithCombo(double now)
    {
        if (now < _comboAt)
        {
            return;
        }

        var next = _comboSkill;

        DropCombo();

        if (!IsAttacking || FindSkill(next) is not { } skill)
        {
            return;
        }

        StopMoving(true);
        Play(skill);
    }
}
