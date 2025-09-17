using System.Xml.Serialization;

namespace OpenWorker.Domain.Batch.Enums;

public enum WaitCreationSequenceType : byte
{
    All,

    [XmlEnum("OnebyOne")]
    OneByOne,

    OnlyOne
}