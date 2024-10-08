﻿using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Security.Authentication;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace PackageIndexer;

public sealed class NuGetFeed(string feedUrl)
{
    public string FeedUrl { get; } = feedUrl;

    public async Task<IReadOnlyList<PackageIdentity>> GetAllPackagesAsync(DateTimeOffset? since = null)
    {
        if (TryGetAzureDevOpsFeed(FeedUrl, out string organization, out string project, out string feed))
            return await GetAllPackagesFromAzureDevOpsFeedAsync(organization, project, feed);

        SourceRepository sourceRepository = Repository.Factory.GetCoreV3(FeedUrl);
        ServiceIndexResourceV3 serviceIndex = await sourceRepository.GetResourceAsync<ServiceIndexResourceV3>();
        string catalogIndexUrl = serviceIndex.GetServiceEntryUri("Catalog/3.0.0")?.ToString();

        if (catalogIndexUrl == null)
            throw new InvalidOperationException("This feed doesn't support enumeration");

        var handler = new HttpClientHandler
        {
            SslProtocols = SslProtocols.Tls12
        };

        using var httpClient = new HttpClient(handler);

        string indexString = await httpClient.GetStringAsync(catalogIndexUrl);
        CatalogIndex index = JsonConvert.DeserializeObject<CatalogIndex>(indexString);

        // Find all pages in the catalog index.
        var pageItems = new ConcurrentBag<CatalogPage>(index.Items);
        var catalogLeaves = new ConcurrentBag<CatalogLeaf>();

        List<Task> fetchLeavesTasks = RunInParallel(async () =>
        {
            while (pageItems.TryTake(out CatalogPage pageItem))
            {
                if (since != null && pageItem.CommitTimeStamp < since.Value)
                    continue;

                int retryCount = 3;
            Retry:
                try
                {
                    // Download the catalog page and deserialize it.
                    string pageString = await httpClient.GetStringAsync(pageItem.Url);
                    CatalogPage page = JsonConvert.DeserializeObject<CatalogPage>(pageString);

                    foreach (CatalogLeaf pageLeafItem in page.Items)
                    {
                        if (pageLeafItem.Type == "nuget:PackageDetails")
                            catalogLeaves.Add(pageLeafItem);
                    }
                }
                catch (Exception ex) when (retryCount > 0)
                {
                    retryCount--;
                    Console.Error.WriteLine($"error: {ex.Message}, retries left = {retryCount}");
                    goto Retry;
                }
            }
        });

        await Task.WhenAll(fetchLeavesTasks);

        return catalogLeaves
            .Select(l => new PackageIdentity(l.Id, NuGetVersion.Parse(l.Version)))
            .Distinct()
            .OrderBy(p => p.Id)
            .ThenBy(p => p.Version)
            .ToArray();

        static List<Task> RunInParallel(Func<Task> work)
        {
            int maxDegreeOfParallelism = Environment.ProcessorCount * 2;
            return Enumerable.Range(0, maxDegreeOfParallelism)
                .Select(i => work())
                .ToList();
        }
    }

    private static async Task<IReadOnlyList<PackageIdentity>> GetAllPackagesFromAzureDevOpsFeedAsync(
        string organization,
        string project,
        string feed
        )
    {
        var result = new List<PackageIdentity>();

        var client = new HttpClient();

        int skip = 0;

        while (true)
        {
            var url = new Uri($"https://feeds.dev.azure.com/{organization}/{project}/_apis/packaging/Feeds/{feed}/packages?api-version=7.1&$skip={skip}", UriKind.Absolute);
            Stream data = await client.GetStreamAsync(url);
            var document = JsonNode.Parse(data);

            int count = document["count"].GetValue<int>();
            if (count == 0)
                break;

            foreach (JsonNode element in document["value"].AsArray())
            {
                string name = element["name"].GetValue<string>();

                foreach (JsonNode versionElement in element["versions"].AsArray())
                {
                    string versionText = versionElement["version"].GetValue<string>();
                    var version = NuGetVersion.Parse(versionText);
                    var identity = new PackageIdentity(name, version);
                    result.Add(identity);
                }
            }

            skip += count;
        }

        return result;
    }

    public async Task<IReadOnlyList<(NuGetVersion, bool)>> GetAllVersionsAsync(string packageId, bool includeUnlisted = false)
    {
        SourceCacheContext cache = NullSourceCacheContext.Instance;
        ILogger logger = NullLogger.Instance;
        CancellationToken cancellationToken = CancellationToken.None;

        SourceRepository repository = Repository.Factory.GetCoreV3(FeedUrl);

        PackageMetadataResource resource = await repository.GetResourceAsync<PackageMetadataResource>();

        IEnumerable<IPackageSearchMetadata> packages = await resource.GetMetadataAsync(
            packageId,
            includePrerelease: true,
            includeUnlisted: includeUnlisted,
            cache,
            logger,
            cancellationToken);

        List<(NuGetVersion, bool)> versions = [];

        foreach (IPackageSearchMetadata package in packages)
        {
            bool isDeprecated = false;
            PackageDeprecationMetadata deprecationMetadata = await package.GetDeprecationMetadataAsync();
            if (deprecationMetadata != null)
                isDeprecated = true;

            versions.Add((package.Identity.Version, isDeprecated));
        }

        return versions;
    }

    public async Task<PackageIdentity> ResolvePackageAsync(string packageId, VersionRange range)
    {
        SourceCacheContext cache = NullSourceCacheContext.Instance;
        ILogger logger = NullLogger.Instance;
        CancellationToken cancellationToken = CancellationToken.None;

        SourceRepository repository = Repository.Factory.GetCoreV3(FeedUrl);
        MetadataResource resource = await repository.GetResourceAsync<MetadataResource>(cancellationToken);
        IEnumerable<NuGetVersion> versions = await resource.GetVersions(packageId, includePrerelease: true, includeUnlisted: true, cache, logger, cancellationToken);
        NuGetVersion bestMatch = versions.FindBestMatch(range, x => x);

        if (bestMatch is null)
            return null;

        return new PackageIdentity(packageId, bestMatch);
    }

    public async Task<PackageArchiveReader> GetPackageAsync(PackageIdentity identity)
    {
        string url = await GetPackageUrlAsync(identity);

        using var httpClient = new HttpClient();
        Stream nupkgStream = await httpClient.GetStreamAsync(url);
        return new PackageArchiveReader(nupkgStream);
    }

    public async Task<bool> TryCopyPackageStreamAsync(PackageIdentity identity, Stream destination)
    {
        string url = await GetPackageUrlAsync(identity);

        int retryCount = 3;
    Retry:
        try
        {
            using var httpClient = new HttpClient();
            Stream nupkgStream = await httpClient.GetStreamAsync(url);
            await nupkgStream.CopyToAsync(destination);
            return true;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex) when (retryCount > 0)
        {
            retryCount--;
            Console.Error.WriteLine($"error: {ex.Message}, retries left = {retryCount}");
            goto Retry;
        }
    }

    public async Task CopyPackageStreamAsync(PackageIdentity identity, Stream destination)
    {
        await TryCopyPackageStreamAsync(identity, destination);
    }

    private async Task<string> GetPackageUrlAsync(PackageIdentity identity)
    {
        SourceRepository sourceRepository = Repository.Factory.GetCoreV3(FeedUrl);
        ServiceIndexResourceV3 serviceIndex = await sourceRepository.GetResourceAsync<ServiceIndexResourceV3>();
        string packageBaseAddress = serviceIndex.GetServiceEntryUri("PackageBaseAddress/3.0.0")?.ToString();

        string id = identity.Id.ToLowerInvariant();
        string version = identity.Version.ToNormalizedString().ToLowerInvariant();
        return $"{packageBaseAddress}{id}/{version}/{id}.{version}.nupkg";
    }

    public Task<Dictionary<string, string[]>> GetOwnerMappingAsync()
    {
        if (FeedUrl != NuGetFeeds.NuGetOrg)
            throw new NotSupportedException("We can only retrieve owner information for nuget.org");

        var httpClient = new HttpClient();
        // TODO - this URL is no longer accessible.
        string url = "https://nugetprodusncazuresearch.blob.core.windows.net/v3-azuresearch-017/owners/owners.v2.json";
        return httpClient.GetFromJsonAsync<Dictionary<string, string[]>>(url)!;
    }

    private static bool TryGetAzureDevOpsFeed(string url, out string organization, out string project, out string feed)
    {
        Match match = Regex.Match(url, """
            https\://pkgs\.dev\.azure\.com/(?<Organization>[^/]+)/(?<Project>[^/]+)/_packaging/(?<Feed>[^/]+)/nuget/v3/index\.json
            """);

        if (match.Success)
        {
            organization = match.Groups["Organization"].Value;
            project = match.Groups["Project"].Value;
            feed = match.Groups["Feed"].Value;
            return true;
        }
        else
        {
            organization = null;
            project = null;
            feed = null;
            return false;
        }
    }

    private abstract class CatalogEntity
    {
        [JsonProperty("@id")]
        public string Url { get; set; }

        [JsonProperty("commitTimeStamp")]
        public DateTime CommitTimeStamp { get; set; }
    }

    private sealed class CatalogIndex : CatalogEntity
    {
        public List<CatalogPage> Items { get; set; }
    }

    private sealed class CatalogPage : CatalogEntity
    {
        public List<CatalogLeaf> Items { get; set; }
    }

    private sealed class CatalogLeaf : CatalogEntity
    {
        [JsonProperty("nuget:id")]
        public string Id { get; set; }

        [JsonProperty("nuget:version")]
        public string Version { get; set; }

        [JsonProperty("@type")]
        public string Type { get; set; }
    }
}
