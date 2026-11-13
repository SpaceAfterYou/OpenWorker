using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

[WireMessage(WireMethods.Write)]
public readonly partial struct PersonInfoValue : IWireReadable<PersonInfoValue>
{
    public PersonInfoValue(string name, Hero hero, AppearanceValue appearanceShape, AppearanceValue appearanceLook)
    {
        Name = name;
        Hero = hero;
        AppearanceShape = appearanceShape;
        AppearanceLook = appearanceLook;
    }

    /// <summary>
    /// What the client sends of a new character.
    /// </summary>
    public static PersonInfoValue Read(ref PacketReader reader)
    {
        var name = reader.ReadUtf16UnicodeString();
        var hero = (Hero)reader.ReadByte();
        var shape = AppearanceValue.Read(ref reader);
        var look = AppearanceValue.Read(ref reader);

        return new PersonInfoValue(name, hero, shape, look);
    }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Name { get; }
    [WireAs(typeof(byte))]
    public Hero Hero { get; }

    public AppearanceValue AppearanceShape { get; init; }
    public AppearanceValue AppearanceLook { get; init; }
}
