namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// What a row of <c>tb_Character_Info</c> gives beyond its columns, which are generated from <c>Structures.json</c>.
/// </summary>
public readonly partial struct CharacterInfoRow
{
    /// <summary>
    /// The hero of the row: its id is the hero times a thousand, plus the variant.
    /// </summary>
    public byte Character => (byte)Math.Floor(Id * 0.001);

    /// <summary>
    /// The twenty skills the hero starts with, a slot without a skill being zero.
    /// </summary>
    public IEnumerable<int> GetSkillList()
    {
        yield return Default_Skill_ID_01;
        yield return Default_Skill_ID_02;
        yield return Default_Skill_ID_03;
        yield return Default_Skill_ID_04;
        yield return Default_Skill_ID_05;
        yield return Default_Skill_ID_06;
        yield return Default_Skill_ID_07;
        yield return Default_Skill_ID_08;
        yield return Default_Skill_ID_09;
        yield return Default_Skill_ID_10;
        yield return Default_Skill_ID_11;
        yield return Default_Skill_ID_12;
        yield return Default_Skill_ID_13;
        yield return Default_Skill_ID_14;
        yield return Default_Skill_ID_15;
        yield return Default_Skill_ID_16;
        yield return Default_Skill_ID_17;
        yield return Default_Skill_ID_18;
        yield return Default_Skill_ID_19;
        yield return Default_Skill_ID_20;
    }
}
