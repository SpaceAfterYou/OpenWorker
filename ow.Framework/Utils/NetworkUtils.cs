using System.Diagnostics;

namespace ow.Framework.Utils
{
    public static class NetworkUtils
    {
        public static void DropSession()
        {
#if !DEBUG
            throw new Exceptions.BadActionException();
#else
            Debug.Assert(false);
#endif
        }
    }
}