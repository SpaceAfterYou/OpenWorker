using OpenWorker.Domain.Persistent;
using OpenWorker.Hotspot.Modules.Login.Components;
using OpenWorker.Hotspot.Modules.Login.Types;

namespace OpenWorker.Hotspot.Extensions;

public static class PersistentExtensions
{
    public static PersonOptionComponent ToPersonOptionComponent(this PersonPersistent value)
    {
        return new PersonOptionComponent
        {
            Collection = value.OptionList.Length switch
            {
                < LoginModuleDefines.PersonOptionCount => value.OptionList
                    .Concat(Enumerable
                        .Repeat((byte)'1', LoginModuleDefines.PersonOptionCount - value.OptionList.Length)
                        .ToArray())
                    .ToArray(),

                > LoginModuleDefines.PersonOptionCount => value.OptionList
                    .Take(LoginModuleDefines.PersonOptionCount)
                    .ToArray(),

                _ => value.OptionList
            }
        };
    }
}