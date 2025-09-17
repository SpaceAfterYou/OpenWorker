using System.Xml.Serialization;

namespace OpenWorker.Domain.Batch.Enums;

public enum ChangeSpawnActionType
{
    [XmlEnum("")]
    Empty,

    [XmlEnum("N_Stand")]
    N_Stand,

    [XmlEnum("N_Spawn")]
    N_Spawn,

    [XmlEnum("B_Stand")]
    B_Stand,

    [XmlEnum("N_Spwan")]
    N_Spwan,

    [XmlEnum("N_B_Mode")]
    N_B_Mode,

    [XmlEnum("0")]
    Zero,

    [XmlEnum("B_Spawn")]
    B_Spawn,

    [XmlEnum("B_Spawn_02")]
    B_Spawn_02,

    [XmlEnum("B_Skill_01")]
    B_Skill_01,

    [XmlEnum("B_Skill_02")]
    B_Skill_02,

    [XmlEnum("B_Skill_05")]
    B_Skill_05,

    [XmlEnum("B_Skill_06")]
    B_Skill_06,

    [XmlEnum("B_A_Stand")]
    B_A_Stand,

    [XmlEnum("B_KB_Start")]
    B_KB_Start,

    [XmlEnum("B_Spwan")]
    B_Spwan,

    [XmlEnum("B_Skill_06_Attack")]
    B_Skill_06_Attack,

    [XmlEnum("B_Idle_01")]
    B_Idle_01,

    [XmlEnum("S_Spawn_02")]
    S_Spawn_02,

    [XmlEnum("B_Action_01")]
    B_Action_01,

    [XmlEnum("N_Idle_01")]
    N_Idle_01,

    [XmlEnum("B_KD_Str_Raise")]
    B_KD_Str_Raise,

    [XmlEnum("B_Skill_08_End")]
    B_Skill_08_End,

    [XmlEnum("N_")]
    N_,

    [XmlEnum("B_N_Mode")]
    B_N_Mode,

    [XmlEnum("B_A_Skill_03")]
    B_A_Skill_03,

    [XmlEnum("B_B_Skill_02")]
    B_B_Skill_02,

    [XmlEnum("B_A_B_Mode_Change")]
    B_A_B_Mode_Change,

    [XmlEnum("B_Idle_02")]
    B_Idle_02,

    [XmlEnum("N_Spawn_01")]
    N_Spawn_01,

    [XmlEnum("N_Spawn_02")]
    N_Spawn_02,

    [XmlEnum("N_Spawn_03")]
    N_Spawn_03,

    [XmlEnum("N_Spawn_04")]
    N_Spawn_04,

    [XmlEnum("N_Spawn_05")]
    N_Spawn_05,

    [XmlEnum("N_Spawn_06")]
    N_Spawn_06,

    [XmlEnum("N_B_Mode_A")]
    N_B_Mode_A,

    [XmlEnum("N_B_Mode_B")]
    N_B_Mode_B,

    [XmlEnum("N_stand")]
    N_stand,

    [XmlEnum("N_Stand_A")]
    N_Stand_A,

    [XmlEnum("N_Stand_B")]
    N_Stand_B
}