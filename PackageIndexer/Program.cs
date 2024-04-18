using CsvHelper;
using CsvHelper.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;

namespace PackageIndexer;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
#if DEBUG
        args = [@"c:\users\gewarren\desktop\Package Index3"];
#endif

        if ((args.Length == 0) || (args.Length > 2))
        {
            string exeName = Path.GetFileNameWithoutExtension(typeof(Program).Assembly.Location);
            Console.Error.Write("error: incorrect number of arguments");
            Console.Error.Write($"usage: {exeName} <download-directory> [preview]");
            return -1;
        }

        //var environmentPath = Environment.GetEnvironmentVariable("APISOFDOTNET_INDEX_PATH");
        //var defaultPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "Catalog");
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
        await GeneratePackageIndexAsync(packageListPath, packagesPath, indexPackagesPath); //, frameworksPath);

        GenerateCSVFiles(indexPackagesPath, csvPath);

        Console.WriteLine($"Completed in {stopwatch.Elapsed}");
        Console.WriteLine($"Peak working set: {Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024):N2} MB");
    }

    private static void GenerateCSVFiles(string indexPackagesPath, string csvPath)
    {
        Console.WriteLine("Generating CSV files from package index.");

        // For each package XML file
        //   For each framework
        //     Map it to a known framework name
        //     Generate a collection of that version + later versions of that framework
        //     (e.g. add 4.7, 4.7.1, 4.7.2, 4.8, 4.8.1 for net462; add 2.1 for netstandard2.0; add 7.0, 8.0, 9.0 for net6.0)
        //     Create a dictionary or add to an existing dictionary *for that version* that will become the CSV file -
        //       pac<num>,[tfm=<tfm>;includeXml=false]<package name>,<package version>
        //       Example: pac01,[tfm=net9.0;includeXml=false]Microsoft.Extensions.Caching.Abstractions,9.0.0-preview.2.24128.5
        // Generate a CSV file from each dictionary

        IList<string> tfms =
            [ "net462", "net47", "net471", "net472", "net48", "net481",
              "net6.0", "net7.0", "net8.0", "net9.0",
              "netstandard2.0", "netstandard2.1"
            ];

        // Initialize each entry in the dictionary.
        Dictionary<string, IList<CsvEntry>> csvDictionary = [];
        foreach (string tfm in tfms)
        {
            csvDictionary.Add(tfm, []);
        }

        int index462 = 1;
        int index47 = 1;
        int index471 = 1;
        int index472 = 1;
        int index48 = 1;
        int index481 = 1;
        int index6 = 1;
        int index7 = 1;
        int index8 = 1;
        int index9 = 1;
        int indexStandard20 = 1;
        int indexStandard21 = 1;

        string frameworkName;

        // Get all XML files (ignores disabled indexes).
        IEnumerable<string> packageIndexFiles = Directory.EnumerateFiles(indexPackagesPath, "*.xml");
        foreach (string packageIndexFile in packageIndexFiles)
        {
            // Read XML file to get each listed framework.
            PackageEntry packageEntry = XmlEntryFormat.ReadPackageEntry(packageIndexFile);

            Console.WriteLine($"Creating CSV entries for package {packageEntry.Name}.");

            // Add to each applicable CSV file.
            foreach (FrameworkEntry tfm in packageEntry.FrameworkEntries)
            {
                switch (tfm.FrameworkName)
                {
                    case "net462":
                        frameworkName = "net462";
                        csvDictionary[frameworkName].Add(CsvEntry.Create(
                            string.Concat("pac", index462++),
                            string.Concat($"[tfm={frameworkName};includeXml=false]", packageEntry.Name),
                            packageEntry.Version
                            ));
                        goto case "net47";
                    case "net47":
                        frameworkName = "net47";
                        csvDictionary[frameworkName].Add(CsvEntry.Create(
                            string.Concat("pac", index47++),
                            string.Concat($"[tfm={frameworkName};includeXml=false]", packageEntry.Name),
                            packageEntry.Version
                            ));
                        goto case "net471";
                    case "net471":
                        frameworkName = "net471";
                        csvDictionary[frameworkName].Add(CsvEntry.Create(
                            string.Concat("pac", index471++),
                            string.Concat($"[tfm={frameworkName};includeXml=false]", packageEntry.Name),
                            packageEntry.Version
                            ));
                        goto case "net472";
                    case "net472":
                        frameworkName = "net472";
                        csvDictionary[frameworkName].Add(CsvEntry.Create(
                            string.Concat("pac", index472++),
                            string.Concat($"[tfm={frameworkName};includeXml=false]", packageEntry.Name),
                            packageEntry.Version
                            ));
                        goto case "net48";
                    case "net48":
                        frameworkName = "net48";
                        csvDictionary[frameworkName].Add(CsvEntry.Create(
                            string.Concat("pac", index48++),
                            string.Concat($"[tfm={frameworkName};includeXml=false]", packageEntry.Name),
                            packageEntry.Version
                            ));
                        goto case "net481";
                    case "net481":
                        frameworkName = "net481";
                        csvDictionary[frameworkName].Add(CsvEntry.Create(
                            string.Concat("pac", index481++),
                            string.Concat($"[tfm={frameworkName};includeXml=false]", packageEntry.Name),
                            packageEntry.Version
                            ));
                        break;
                    case "net6.0":
                        frameworkName = "net6.0";
                        csvDictionary[frameworkName].Add(CsvEntry.Create(
                            string.Concat("pac", index6++),
                            string.Concat($"[tfm={frameworkName};includeXml=false]", packageEntry.Name),
                            packageEntry.Version
                            ));
                        break;
                    case "net7.0":
                        frameworkName = "net7.0";
                        csvDictionary[frameworkName].Add(CsvEntry.Create(
                            string.Concat("pac", index7++),
                            string.Concat($"[tfm={frameworkName};includeXml=false]", packageEntry.Name),
                            packageEntry.Version
                            ));
                        break;
                    case "net8.0":
                        frameworkName = "net8.0";
                        csvDictionary[frameworkName].Add(CsvEntry.Create(
                            string.Concat("pac", index8++),
                            string.Concat($"[tfm={frameworkName};includeXml=false]", packageEntry.Name),
                            packageEntry.Version
                            ));
                        break;
                    case "net9.0":
                        frameworkName = "net9.0";
                        csvDictionary[frameworkName].Add(CsvEntry.Create(
                            string.Concat("pac", index9++),
                            string.Concat($"[tfm={frameworkName};includeXml=false]", packageEntry.Name),
                            packageEntry.Version
                            ));
                        break;
                    case "netstandard2.0":
                        frameworkName = "netstandard2.0";
                        csvDictionary[frameworkName].Add(CsvEntry.Create(
                            string.Concat("pac", indexStandard20++),
                            string.Concat($"[tfm={frameworkName};includeXml=false]", packageEntry.Name),
                            packageEntry.Version
                            ));
                        break;
                    case "netstandard2.1":
                        frameworkName = "netstandard2.1";
                        csvDictionary[frameworkName].Add(CsvEntry.Create(
                            string.Concat("pac", indexStandard21++),
                            string.Concat($"[tfm={frameworkName};includeXml=false]", packageEntry.Name),
                            packageEntry.Version
                            ));
                        break;
                    default:
                        Console.WriteLine($"Ignoring target framework {tfm.FrameworkName}.");
                        break;
                }
            }
        }

        // Create the directory.
        Directory.CreateDirectory(csvPath);

        foreach (KeyValuePair<string, IList<CsvEntry>> tfm in csvDictionary)
        {
            string filePath = Path.Combine(csvPath, string.Concat(tfm.Key, ".csv"));

            // Delete the file if it already exists.
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using var writer = new StreamWriter(filePath);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                // Don't write the header.
                HasHeaderRecord = false,
            };
            using var csv = new CsvWriter(writer, config);
            csv.WriteRecords(tfm.Value);
        }
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
