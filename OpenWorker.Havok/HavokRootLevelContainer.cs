using OpenWorker.Havok.Attributes;

namespace OpenWorker.Havok;

[HavokSerializeClass("hkRootLevelContainer", 0)]
public class HavokRootLevelContainer
{
    [HavokSerializeMember("name")]
    public string Name { get; set; } = string.Empty;

    [HavokSerializeMember("className")]
    public string ClassName { get; set; } = string.Empty;

    [HavokSerializeMember("variant")]
    public HavokVariant Variant { get; set; } = HavokVariant.Empty;
}
