using System.Collections.ObjectModel;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.GateServer.Gameplay;

/// <summary>
/// What the character creation screen offers for each hero.
/// </summary>
public sealed class PersonCreationCatalog(
    ReadOnlyCollection<CharacterInfoRow> characters,
    ReadOnlyCollection<CustomizeHairRow> hair,
    ReadOnlyCollection<CustomizeEyesRow> eyes,
    ReadOnlyCollection<CustomizeSkinRow> skin)
{
    public bool TryGetCharacter(Hero hero, out CharacterInfoRow character)
    {
        var id = (int)hero * 1000;

        character = characters.FirstOrDefault(e => e.Id == id);

        return character.Id != 0;
    }

    /// <summary>
    /// The client sends 0 for everything the player has not touched and shows the default look for it.
    /// </summary>
    public bool IsOffered(Hero hero, AppearanceValue appearance)
    {
        var hairRow = hair.FirstOrDefault(e => e.Id == (byte)hero);
        var eyesRow = eyes.FirstOrDefault(e => e.Id == (byte)hero);
        var skinRow = skin.FirstOrDefault(e => e.Id == (byte)hero);

        return IsOffered(appearance.HairStyle,
                   hairRow.Field11, hairRow.Field12, hairRow.Field13, hairRow.Field14, hairRow.Field15,
                   hairRow.Field16, hairRow.Field17, hairRow.Field18, hairRow.Field19, hairRow.Field20) &&
               IsOffered(appearance.HairColor,
                   hairRow.Field41, hairRow.Field42, hairRow.Field43, hairRow.Field44, hairRow.Field45,
                   hairRow.Field46, hairRow.Field47, hairRow.Field48, hairRow.Field49, hairRow.Field50) &&
               IsOffered(appearance.EyeColor,
                   eyesRow.Field21, eyesRow.Field22, eyesRow.Field23, eyesRow.Field24, eyesRow.Field25,
                   eyesRow.Field26, eyesRow.Field27, eyesRow.Field28, eyesRow.Field29, eyesRow.Field30) &&
               IsOffered(appearance.SkinColor,
                   skinRow.Field21, skinRow.Field22, skinRow.Field23, skinRow.Field24, skinRow.Field25,
                   skinRow.Field26, skinRow.Field27, skinRow.Field28, skinRow.Field29, skinRow.Field30);
    }

    public static bool IsOffered(in CharacterInfoRow character, int outfit)
    {
        return IsOffered(
            outfit,
            character.Default_Cloth_ID_01,
            character.Default_Cloth_ID_02,
            character.Default_Cloth_ID_03,
            character.Default_Cloth_ID_04);
    }

    private static bool IsOffered(int value, params ReadOnlySpan<int> offered)
    {
        return value == 0 || offered.Contains(value);
    }
}
