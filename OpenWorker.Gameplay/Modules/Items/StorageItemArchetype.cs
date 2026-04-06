using System.Collections;
using Arch.Core;

namespace OpenWorker.Gameplay.Modules.Items;

internal readonly record struct StorageItemArchetype
{
    public BitArray Identifier { get; init;  }
    public ComponentType[] Components { get; init;  }
}