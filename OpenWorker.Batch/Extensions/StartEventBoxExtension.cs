using System.Numerics;
using OpenWorker.Batch.Entities;
using OpenWorker.Batch.Entities.Basic;
using OpenWorker.Domain.Batch.Enums;

namespace OpenWorker.Batch.Extensions;

public static class StartEventBoxExtension
{
    public static Vector3 GetRandomPosition(this BasicEntity self)
    {
        var x = Random.Shared.NextSingle() * (self.PosTopLeft.X - self.PosBottomRight.X) + self.PosBottomRight.X;
        var y = Random.Shared.NextSingle() * (self.PosTopLeft.Y - self.PosBottomRight.Y) + self.PosBottomRight.Y;
        var z = Random.Shared.NextSingle() * (self.PosTopLeft.Z - self.PosBottomRight.Z) + self.PosBottomRight.Z;

        return new Vector3(x, y, z);
    }

    public static Vector3 GetPosition(this StartEventBox self)
    {
        return self.SpawnType != SpawnType.Random ? self.PosTopLeft : self.GetRandomPosition();
    }

    public static Vector3 GetPosition(this MonsterSpawnBox self)
    {
        return self.CreationPositionType switch
        {
            CreationPositionType.Random => self.GetRandomPosition(),
            CreationPositionType.Center => Vector3.Lerp(self.PosTopLeft, self.PosBottomRight, 0.5f),
            CreationPositionType.Sort => throw new NotImplementedException(),
            
            _ => throw new NotSupportedException("Unknown CreationPositionType")
        };
    }
}