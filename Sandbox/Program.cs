using DotNet.DocsTools.OspoClientServices;

var client = await OspoClientFactory.CreateAsync(true);

var link = await client.GetAsync("IEvangelist");

if (link is not null)
{
    _ = link;
}
