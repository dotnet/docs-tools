using Microsoft.DotnetOrg.Ospo;

var azureAccessToken = Environment.GetEnvironmentVariable("AZURE_ACCESS_TOKEN");

var client = new OspoClient(azureAccessToken!, true);

var link = client != null ? await client.GetAsync("IEvangelist") : null;

if (link is not null)
{
    _ = link;
}
