using System.Diagnostics;
using System.Xml.Linq;

namespace PackageIndexer;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
#if DEBUG
        args = [@"c:\users\gewarren\desktop\Package Index 1106", "preview"];
#endif

        if ((args.Length == 0) || (args.Length > 2))
        {
            string exeName = Path.GetFileNameWithoutExtension(typeof(Program).Assembly.Location);
            Console.Error.Write("error: incorrect number of arguments");
            Console.Error.Write($"usage: {exeName} <download-directory> [preview]");
            return -1;
        }

        string rootPath = args[0];

        bool usePreviewVersions = false;
        if (args.Length > 1)
        {
            if (string.Equals(args[1], "preview", StringComparison.InvariantCultureIgnoreCase))
                usePreviewVersions = true;
        }

        bool success = true;

        try
        {
            await RunAsync(rootPath, usePreviewVersions);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            success = false;
        }

        return success ? 0 : -1;
    }

    private static async Task RunAsync(string rootPath, bool usePreviewVersions)
    {
        string packagesPath = Path.Combine(rootPath, "packages");
        string packageListPath = Path.Combine(packagesPath, "packages.xml");
        string indexPath = Path.Combine(rootPath, "index");
        string indexPackagesPath = Path.Combine(indexPath, "packages");
        string csvPath = Path.Combine(rootPath, "csvFiles");

        Stopwatch stopwatch = Stopwatch.StartNew();

        await DownloadDotnetPackageListAsync(packageListPath, usePreviewVersions);
        await GeneratePackageIndexAsync(packageListPath, packagesPath, indexPackagesPath);

        CsvUtils.GenerateCSVFiles(indexPackagesPath, csvPath);

        Console.WriteLine($"Completed in {stopwatch.Elapsed}");
        Console.WriteLine($"Peak working set: {Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024):N2} MB");
    }

    private static async Task DownloadDotnetPackageListAsync(string packageListPath, bool usePreviewVersions)
    {
        if (!File.Exists(packageListPath))
            await DotnetPackageIndex.CreateAsync(packageListPath, usePreviewVersions);
    }

    private static async Task GeneratePackageIndexAsync(
        string packageListPath,
        string packagesPath,
        string indexPackagesPath
        )
    {
        Directory.CreateDirectory(packagesPath);
        Directory.CreateDirectory(indexPackagesPath);

        var document = XDocument.Load(packageListPath);
        Directory.CreateDirectory(packagesPath);

        (string Id, string Version)[] packages = document.Root!.Elements("package")
            .Select(e => (
                Id: e.Attribute("id")!.Value,
                Version: e.Attribute("version")!.Value))
            .ToArray();

        var nightlies = new NuGetFeed(NuGetFeeds.NightlyLatest);
        var nuGetOrg = new NuGetFeed(NuGetFeeds.NuGetOrg);
        var nugetStore = new NuGetStore(packagesPath, nightlies, nuGetOrg);
        var packageIndexer = new PackageIndexer(nugetStore); //, frameworkLocators);

        bool retryIndexed = false;
        bool retryDisabled = false;
        bool retryFailed = false;

        foreach ((string id, string version) in packages)
        {
            string path = Path.Join(indexPackagesPath, $"{id}-{version}.xml");
            string disabledPath = Path.Join(indexPackagesPath, $"{id}-all.disabled");
            string failedVersionPath = Path.Join(indexPackagesPath, $"{id}-{version}.failed");

            bool alreadyIndexed = !retryIndexed && File.Exists(path) ||
                                 !retryDisabled && File.Exists(disabledPath) ||
                                 !retryFailed && File.Exists(failedVersionPath);

            if (alreadyIndexed)
            {
                if (File.Exists(path))
                    Console.WriteLine($"Package {id} {version} already indexed.");

                if (File.Exists(disabledPath))
                    nugetStore.DeleteFromCache(id, version);
            }
            else
            {
                Console.WriteLine($"Indexing {id} {version}...");
                try
                {
                    PackageEntry packageEntry = await packageIndexer.Index(id, version);
                    if (packageEntry is null)
                    {
                        Console.WriteLine($"Not a library package.");
                        File.WriteAllText(disabledPath, string.Empty);
                        nugetStore.DeleteFromCache(id, version);
                    }
                    else
                    {
                        using (FileStream stream = File.Create(path))
                            packageEntry.Write(stream);

                        File.Delete(disabledPath);
                        File.Delete(failedVersionPath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed: {ex}");
                    File.Delete(disabledPath);
                    File.Delete(path);
                    File.WriteAllText(failedVersionPath, ex.ToString());
                }
            }
        }
    }
}
