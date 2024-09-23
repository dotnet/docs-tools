using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DotNetDocs.RepoMan;

internal static class Extensions
{

    /// <summary>
    /// Log an error and return a <see cref="BadRequestObjectResult" />.
    /// </summary>
    /// <param name="logger">The logger instance to log the error.</param>
    /// <param name="error">The error string.</param>
    /// <returns>A result with the error string as content.</returns>
    public static BadRequestObjectResult LogFailure(this ILogger logger, string error)
    {
#pragma warning disable CA2254 // Template should be a static expression
        logger.LogError(error);
#pragma warning restore CA2254 // Template should be a static expression
        return new BadRequestObjectResult(error);
    }

    /// <summary>
    /// Gets the value of a key in the header dictionary.
    /// </summary>
    /// <param name="headers">The header instance.</param>
    /// <param name="key">The name of the header.</param>
    /// <param name="value">The value.</param>
    /// <returns>true if the <see cref="IHeaderDictionary" /> contains the key and the value was retrieved; otherwise, false.</returns>
    public static bool TryGetSingleString(this IHeaderDictionary headers, string key, out string value)
    {
        if (headers.TryGetValue(key, out Microsoft.Extensions.Primitives.StringValues headerValues))
        {
            if (headerValues.FirstOrDefault() is string stringValue)
            {
                value = stringValue;
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    /// <summary>
    /// Gets the value of a key in the header dictionary.
    /// </summary>
    /// <param name="headers">The header instance.</param>
    /// <param name="key">The name of the header.</param>
    /// <param name="value">The value.</param>
    /// <returns>true if the <see cref="IHeaderDictionary" /> contains the key and the value was retrieved; otherwise, false.</returns>
    public static bool TryGetSingleInt(this IHeaderDictionary headers, string key, out int value)
    {
        if (headers.TryGetValue(key, out Microsoft.Extensions.Primitives.StringValues headerValues))
        {
            if (headerValues.FirstOrDefault() is string stringValue)
            {
                if (int.TryParse(stringValue, out value))
                    return true;
            }
        }

        value = 0;
        return false;
    }

    public static bool OverrideWithEnvironmentVariable(this bool defaultValue, string name)
    {
        string? envValue = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);

        if (envValue is null)
            return defaultValue;
        else
            return bool.Parse(envValue);
    }
}
