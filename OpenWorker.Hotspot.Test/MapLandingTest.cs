using System.Globalization;
using System.Numerics;
using System.Xml.Linq;
using OpenWorker.Batch;
using OpenWorker.Batch.Entities;
using OpenWorker.Batch.Extensions;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Where a way into a map lands: the way back from a maze names the exit box of the maze's portal in the district.
/// </summary>
[TestClass]
public sealed class MapLandingTest
{
    private const int DefaultStart = 1000301;
    private const int PortalExit = 1000303;

    private static string Vector(Vector3 v) => string.Create(CultureInfo.InvariantCulture, $"{v.X:0.000},{v.Y:0.000},{v.Z:0.000}");

    private static string Basic(int id, Vector3 low, Vector3 high) =>
        $"""<m_ID type="0" value="{id}" /> <m_iGenerateID type="0" value="{id}" /> <iLayerBitmask type="4" value="Common" /> <m_vPosTopLeft type="7" value="{Vector(low)}" /> <m_vPosBottomRight type="7" value="{Vector(high)}" /> <m_fRotation type="2" value="0.0000" /> <m_vSize type="7" value="{Vector(high - low)}" /> <m_bShowCustomEntity type="5" value="TRUE" /> <m_ColorLDR type="132" value="0,0,0,255" />""";

    private static EventBox District() => new(XElement.Parse(
        "<Batchs eventtype=\"EventBox\">" +
        $"""<VStartEventBox boxtype="eEventBoxType_Start"> {Basic(DefaultStart, new Vector3(10000f, 10000f, 90f), new Vector3(10100f, 10100f, 90f))} <m_eSpawnType type="4" value="Random" /> </VStartEventBox>""" +
        $"""<VPortalExitBox boxtype="eEventBoxType_PortalExit"> {Basic(PortalExit, new Vector3(40000f, 29500f, 250f), new Vector3(40100f, 29600f, 250f))} <m_iParentPortalBoxID type="0" value="0" /> </VPortalExitBox>""" +
        "</Batchs>"));

    [TestMethod]
    public void AWayBackLandsAtTheExitBoxOfThePortalItNames()
    {
        var district = District();

        var landing = district.LandingOf(PortalExit);

        Assert.IsInstanceOfType<PortalExitBox>(landing, "the exit box of the portal, not the start of the district");

        var position = landing.GetLandingPosition();

        Assert.IsTrue(position.X is >= 40000f and <= 40100f && position.Y is >= 29500f and <= 29600f, $"inside the exit box: {position}");
        Assert.IsInstanceOfType<StartEventBox>(district.LandingOf(DefaultStart), "a start box is landed at too");
        Assert.IsNull(district.LandingOf(12345), "an id the map does not have");
    }
}
