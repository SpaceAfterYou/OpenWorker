using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot.Modules.Login.Types;

namespace OpenWorker.Gameplay.Modules.Login.Extensions;

public static class PersonOptionPersistentExtension
{
    /// <summary>
    /// The options of the client as the account keeps them, padded with the default (on) or cut to the length the client reads.
    /// </summary>
    public static PersonOptionComponent ToPersonOptionComponent(this byte[] options)
    {
        return new PersonOptionComponent
        {
            Collection = options.Length switch
            {
                < LoginModuleDefines.PersonOptionCount => options
                    .Concat(Enumerable
                        .Repeat((byte)'1', LoginModuleDefines.PersonOptionCount - options.Length)
                        .ToArray())
                    .ToArray(),

                > LoginModuleDefines.PersonOptionCount => options
                    .Take(LoginModuleDefines.PersonOptionCount)
                    .ToArray(),

                _ => options
            }
        };
    }
}
