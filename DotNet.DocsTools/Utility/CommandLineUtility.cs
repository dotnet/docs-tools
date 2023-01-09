namespace DotNet.DocsTools.Utility;

/// <summary>
/// This class contains utilities used for our command line tools.
/// </summary>
public static class CommandLineUtility
{
    /// <summary>
    /// Retrieve an environment variable, or default.
    /// </summary>
    /// <param name="item">The environment variable to retrieve.</param>
    /// <param name="errorMessage">The error message to retrieve</param>
    /// <param name="defaultValue">The default value to use if the environment
    /// variable isn't present.</param>
    /// <returns>The value</returns>
    /// <remarks>
    /// Throws an InvalidOperationException when the environment variable is not
    /// found, and no default is given.
    /// </remarks>
    public static string GetEnvVariable(string item, string errorMessage, string? defaultValue)
    {
        // I don't have tests on this, but I've tested each 
        // application where it's used. I haven't figured out how to test this without
        // mocking the "GetEnvironmentVariable" call. That seems overkill.
        // If you change it, run more tests.
        var value = Environment.GetEnvironmentVariable(item);
        if (string.IsNullOrWhiteSpace(value))
        {
            if (!string.IsNullOrWhiteSpace(defaultValue))
            {
                return defaultValue;
            }
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                Console.WriteLine(errorMessage);
            }
            throw new InvalidOperationException(errorMessage);
        }
        return value;
    }
}
