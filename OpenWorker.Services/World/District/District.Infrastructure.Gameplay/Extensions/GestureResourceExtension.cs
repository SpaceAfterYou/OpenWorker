using OpenWorker.Domain.DatabaseModel.Resources;
using SoulWorkerResearch.SoulCore.Defines;

namespace OpenWorker.Services.District.Infrastructure.Gameplay.Extensions;

internal static class GestureResourceExtension
{
    internal static bool IsValid(this GestureResource @this, Class @class)
    {
        // TODO: Check person class
        //    gesture.Class == person.Class || gesture.Class == Class.None
        return true;
    }
}
