using DotNet.DocsTools.OspoClientServices;

var client = await OspoClientFactory.CreateAsync();

var link = await client.GetAsync("IEvangelist");

if (link is not null)
{
    _ = link;
}
