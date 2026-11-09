using OpenWorker.Hotspot.Modules.Persons.Enums;

namespace OpenWorker.Gameplay.Modules.Login;

public static class LoginRules
{
    private const int MinPersonNameLength = 2;
    private const int MaxPersonNameLength = 20;
    private const int SecondPasswordLength = 6;
    private const int MaxDigitRun = 3;

    public static bool IsValidPersonName(string name)
    {
        return name.Length is >= MinPersonNameLength and <= MaxPersonNameLength && name.All(char.IsLetterOrDigit);
    }

    /// <summary>
    /// A second password has six digits without three repeated or consecutive ones in a row.
    /// </summary>
    public static PersonErrorCode? ValidateSecondPassword(string password)
    {
        if (password.Length != SecondPasswordLength || !password.All(char.IsAsciiDigit))
        {
            return PersonErrorCode.SecondPasswordInvalid;
        }

        int same = 1, up = 1, down = 1;

        for (var i = 1; i < password.Length; i++)
        {
            same = password[i] == password[i - 1] ? same + 1 : 1;
            up = password[i] == password[i - 1] + 1 ? up + 1 : 1;
            down = password[i] == password[i - 1] - 1 ? down + 1 : 1;

            if (same >= MaxDigitRun)
            {
                return PersonErrorCode.SecondPasswordRepeatedDigits;
            }

            if (up >= MaxDigitRun || down >= MaxDigitRun)
            {
                return PersonErrorCode.SecondPasswordSequentialDigits;
            }
        }

        return null;
    }

    /// <summary>
    /// The client's code for a trade password that is not four of the digits 0 to 7.
    /// </summary>
    public const ushort TradePasswordInvalid = 59504;

    /// <summary>
    /// The client's code for a trade password of one digit four times.
    /// </summary>
    public const ushort TradePasswordRepeated = 59505;

    private const int TradePasswordLength = 4;

    /// <summary>
    /// A trade password has four of the digits 0 to 7 (the keys of the client's pad), not one digit four times. Returns
    /// zero for a password that may be kept, otherwise the code of the refusal.
    /// </summary>
    public static ushort ValidateTradePassword(string password)
    {
        if (password.Length != TradePasswordLength || !password.All(e => e is >= '0' and <= '7'))
        {
            return TradePasswordInvalid;
        }

        return password.All(e => e == password[0]) ? TradePasswordRepeated : (ushort)0;
    }
}
