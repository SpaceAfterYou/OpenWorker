using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Messages.Response.Person.Components;
using OpenWorker.Hotspot.Modules.Persons.Components;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

public readonly struct PersonInfoValue
{
    public PersonInfoValue(PersonInfoComponent component, AppearanceComponent appearance)
    {
        Name = component.Name;
        Hero = component.Hero;

        AppearanceShape = AppearanceValue.CreateShape(appearance);
        AppearanceLook = AppearanceValue.CreateLook(appearance);
    }

    public PersonInfoValue(BinaryReader reader)
    {
        Name = reader.ReadUtf8UnicodeString();
        Hero = reader.ReadHero();

        AppearanceShape = new AppearanceValue(reader);
        AppearanceLook = new AppearanceValue(reader);
    }

    public string Name { get; }
    public Hero Hero { get; }

    public AppearanceValue AppearanceShape { get; init; }
    public AppearanceValue AppearanceLook { get; init; }
}