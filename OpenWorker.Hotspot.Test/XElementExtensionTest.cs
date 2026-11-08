using System.Drawing;
using System.Globalization;
using System.Numerics;
using System.Xml.Linq;
using OpenWorker.Batch.Extensions;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The batch files are written with a dot and no thousands separator, whatever the machine of the server is set to. Under
/// ru-RU "1.5" does not parse at all, and under de-DE the dot is the group separator, so it parses as 15.
/// </summary>
[TestClass]
public sealed class XElementExtensionTest
{
    private static XElement Box(string name, string value)
    {
        return new XElement("Box", new XElement(name, new XAttribute("value", value)));
    }

    /// <summary>
    /// Runs the action with the culture of the thread set, and puts the culture back.
    /// </summary>
    private static void Under(string culture, Action action)
    {
        var saved = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);

            // A runtime without the culture data answers with the invariant culture, and the test would prove nothing.
            Assert.AreEqual(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

            action();
        }

        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    [TestMethod]
    [DataRow("ru-RU")]
    [DataRow("de-DE")]
    public void ParsesAFloatWithADotWhateverTheCultureIs(string culture)
    {
        Under(culture, () =>
        {
            Assert.AreEqual(1.5f, Box("m_f", "1.5").GetSingle("m_f"));
            Assert.AreEqual(-0.25f, Box("m_f", "-0.25").GetSingle("m_f"));
        });
    }

    [TestMethod]
    [DataRow("ru-RU")]
    [DataRow("de-DE")]
    public void ParsesTheIntegersWhateverTheCultureIs(string culture)
    {
        Under(culture, () =>
        {
            Assert.AreEqual(-12, Box("m_i", "-12").GetInt32("m_i"));
            Assert.AreEqual(34, Box("m_i", "34").GetUInt32("m_i"));
            Assert.AreEqual((short)-5, Box("m_i", "-5").GetInt16("m_i"));
            Assert.AreEqual((byte)200, Box("m_i", "200").GetByte("m_i"));
        });
    }

    [TestMethod]
    [DataRow("ru-RU")]
    [DataRow("de-DE")]
    public void ParsesAVectorAndAColorWhateverTheCultureIs(string culture)
    {
        Under(culture, () =>
        {
            Assert.AreEqual(new Vector3(1.5f, -2.25f, 3f), Box("m_v", "1.5,-2.25,3").GetVector3("m_v"));
            Assert.AreEqual(Color.FromArgb(alpha: 4, red: 1, green: 2, blue: 3).ToArgb(), Box("m_c", "1,2,3,4").GetColor("m_c").ToArgb());
        });
    }

    [TestMethod]
    public void ParsesTheSameUnderTheInvariantCulture()
    {
        var saved = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            Assert.AreEqual(1.5f, Box("m_f", "1.5").GetSingle("m_f"));
        }

        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }
}
