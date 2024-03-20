using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace RepoMan;

public static class ILoggerExtensions
{
    public static void LogDebugger(this ILogger logger, string msg, params object[] args)
    {
        if (Environment.GetEnvironmentVariable("logdebug", EnvironmentVariableTarget.Process) != null)
            logger.LogInformation(msg, args);
        else
            logger.LogDebug(msg, args);
    }
}
