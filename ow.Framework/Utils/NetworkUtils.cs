using ow.Framework.Exceptions;
using ow.Framework.IO.Network.Sync.Exceptions;
using System.Diagnostics;

namespace ow.Framework.Utils
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