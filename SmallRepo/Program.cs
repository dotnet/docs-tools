
using Microsoft.DotnetOrg.Ospo;

try
{
    var azureAccessToken = CoalesceEnvVar(("ImportOptions__ApiKeys__AzureAccessToken", "AZURE_ACCESS_TOKEN"), false);

    var ospoClient = new OspoClient(azureAccessToken, false);

    var result = ospoClient.GetAsync("BillWagner");


    var id = await result;

    if (id is not null)
        Console.WriteLine("Success");
    else
        Console.WriteLine("Failure");
} catch (Exception e)
{
    Console.WriteLine("Exception failure");
    Console.WriteLine(e.ToString());
}

    static string CoalesceEnvVar((string preferredKey, string fallbackKey) keys, bool required = true)
    {
        var (preferredKey, fallbackKey) = keys;

        // Attempt the preferred key first.
        var value = Environment.GetEnvironmentVariable(preferredKey);
        if (string.IsNullOrWhiteSpace(value))
        {
            // If the preferred key is not set, try the fallback key.
            value = Environment.GetEnvironmentVariable(fallbackKey);
            Console.WriteLine($"{(string.IsNullOrWhiteSpace(value) ? $"Neither {preferredKey} or {fallbackKey} found" : $"Found {fallbackKey}")}");
        }
        else
        {
            Console.WriteLine($"Found value for {preferredKey}");
        }

        // If neither key is set, throw an exception if required.
        if (string.IsNullOrWhiteSpace(value) && required)
        {
            throw new Exception(
                $"Missing env var, checked for both: {preferredKey} and {fallbackKey}.");
        }

        return value!;
    }
