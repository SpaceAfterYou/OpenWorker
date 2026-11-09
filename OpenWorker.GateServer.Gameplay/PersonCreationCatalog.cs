using System.Collections.ObjectModel;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Res;
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
    private readonly RowIndex<int, CharacterInfoRow> _characters = characters.IndexBy(e => (int)e.Id);
    private readonly RowIndex<byte, CustomizeHairRow> _hair = hair.IndexBy(e => e.Id);
    private readonly RowIndex<byte, CustomizeEyesRow> _eyes = eyes.IndexBy(e => e.Id);
    private readonly RowIndex<byte, CustomizeSkinRow> _skin = skin.IndexBy(e => e.Id);

    public bool TryGetCharacter(Hero hero, out CharacterInfoRow character)
    {
        var id = (int)hero * 1000;

        character = _characters.GetOrDefault(id);

        return character.Id != 0;
    }

    /// <summary>
    /// The client sends 0 for everything the player has not touched and shows the default look for it.
    /// </summary>
    public bool IsOffered(Hero hero, AppearanceValue appearance)
    {
        var hairRow = _hair.GetOrDefault((byte)hero);
        var eyesRow = _eyes.GetOrDefault((byte)hero);
        var skinRow = _skin.GetOrDefault((byte)hero);

        return IsOffered(appearance.HairStyle,
                   hairRow.Style1, hairRow.Style2, hairRow.Style3, hairRow.Style4, hairRow.Style5,
                   hairRow.Style6, hairRow.Style7, hairRow.Style8, hairRow.Style9, hairRow.Style10) &&
               IsOffered(appearance.HairColor,
                   hairRow.Color1, hairRow.Color2, hairRow.Color3, hairRow.Color4, hairRow.Color5,
                   hairRow.Color6, hairRow.Color7, hairRow.Color8, hairRow.Color9, hairRow.Color10) &&
               IsOffered(appearance.EyeColor,
                   eyesRow.Color1, eyesRow.Color2, eyesRow.Color3, eyesRow.Color4, eyesRow.Color5,
                   eyesRow.Color6, eyesRow.Color7, eyesRow.Color8, eyesRow.Color9, eyesRow.Color10) &&
               IsOffered(appearance.SkinColor,
                   skinRow.Color1, skinRow.Color2, skinRow.Color3, skinRow.Color4, skinRow.Color5,
                   skinRow.Color6, skinRow.Color7, skinRow.Color8, skinRow.Color9, skinRow.Color10);
    }

    /// <summary>
    /// The four costumes of the creation screen, the ones of <c>tb_Create_Cloth</c>; the first is the one the client sends
    /// when the player has not chosen.
    /// </summary>
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
