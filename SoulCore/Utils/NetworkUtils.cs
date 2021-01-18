using SoulCore.Exceptions;
using SoulCore.IO.Network.Sync.Exceptions;
using System.Diagnostics;

namespace SoulCore.Utils
{
    public static class NetworkUtils
    {
        public static void DropBadAction()
        {
            Debug.Assert(false);
            throw new BadActionException();
        }

        public static void DropNetwork()
        {
            Debug.Assert(false);
            throw new NetworkException();
        }
    }
}
