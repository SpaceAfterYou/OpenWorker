using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

[WireMessage(WireMethods.Write)]
public readonly partial struct PersonInfoValue
{
    public PersonInfoValue(string name, Hero hero, AppearanceValue appearanceShape, AppearanceValue appearanceLook)
    {
        Name = name;
        Hero = hero;
        AppearanceShape = appearanceShape;
        AppearanceLook = appearanceLook;
    }

    public PersonInfoValue(BinaryReader reader)
    {
        Name = reader.ReadUtf8UnicodeString();
        Hero = reader.ReadHero();

        AppearanceShape = new AppearanceValue(reader);
        AppearanceLook = new AppearanceValue(reader);
    }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Name { get; }
    [WireAs(typeof(byte))]
    public Hero Hero { get; }

    public AppearanceValue AppearanceShape { get; init; }
    public AppearanceValue AppearanceLook { get; init; }
}
