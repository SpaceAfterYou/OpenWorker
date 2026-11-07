namespace OpenWorker.Hotspot.Modules.Persons.Enums;

public enum PersonErrorCode : ushort
{
    Failed = 51001,
    NoEmptySlot = 51003,
    InvalidName = 51004,
    NameInUse = 51005,
    CreateFailed = 51011,
    LeagueMaster = 51012,

    SecondPasswordInvalid = 59501,
    SecondPasswordRepeatedDigits = 59502,
    SecondPasswordSequentialDigits = 59503
}
