using Microsoft.Extensions.Logging;
using System;

namespace SoulCore.Utils
{
    public static class CommonUtils
    {
        public static void PrintEnvironment(ILogger logger)
        {
            logger.LogDebug("Environment:");
            logger.LogDebug($"CurrentDirectory: {Environment.CurrentDirectory}");
            logger.LogDebug($"OS: {Environment.OSVersion}");
            logger.LogDebug($".NET: {Environment.Version}");
            logger.LogDebug($"x64: {Environment.Is64BitProcess}");
            logger.LogDebug($"Processors: {Environment.ProcessorCount}");
        }
    }
}
