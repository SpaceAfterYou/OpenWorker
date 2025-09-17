namespace OpenWorker.Domain.Batch.Enums;

public enum PointType : byte
{
    None,
    StartReturnWait,
    StartReturnGo,
    Wait,
    Return,
    Delete
}