using DotNet.DocsTools.OspoClientServices;

// The actual values should be secrets. DO NOT CHECK THEM IN!!!!
var clientID = args.Length == 3 ? args[0] : null;
var tenantID = args.Length == 3 ? args[1] : null;
var resourceAudience = args.Length == 3 ? args[2] : null;

var client = await OspoClientFactory.CreateAsync(clientID!, tenantID!, resourceAudience!, true);

var link = client != null ? await client.GetAsync("IEvangelist") : null;

if (link is not null)
{
    _ = link;
}
