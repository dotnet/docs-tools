using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;
using CsvHelper;
using CsvHelper.Configuration;

namespace PackageIndexer;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
#if DEBUG
        args = [@"c:\users\gewarren\desktop\Package Index 1030", "preview"];
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
        await GeneratePackageIndexAsync(packageListPath, packagesPath, indexPackagesPath); //, frameworksPath);

        CsvUtils.GenerateCSVFiles(indexPackagesPath, csvPath);

        Console.WriteLine($"Completed in {stopwatch.Elapsed}");
        Console.WriteLine($"Peak working set: {Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024):N2} MB");
    }

    //private static void GenerateCSVFiles(string indexPackagesPath, string csvPath)
    //{
    //    Console.WriteLine("Generating CSV files from package index.");

    //    // For each package XML file
    //    //   For each framework
    //    //     Map it to a known framework name
    //    //     Generate a collection of that version + later versions of that framework
    //    //     (e.g. add 4.7, 4.7.1, 4.7.2, 4.8, 4.8.1 for net462; add 2.1 for netstandard2.0; add 7.0, 8.0, 9.0 for net6.0)
    //    //     Create a dictionary or add to an existing dictionary *for that version* that will become the CSV file -
    //    //       pac<num>,[tfm=<tfm>;includeXml=false]<package name>,<package version>
    //    //       Example: pac01,[tfm=net9.0;includeXml=false]Microsoft.Extensions.Caching.Abstractions,9.0.0-preview.2.24128.5
    //    // Generate a CSV file from each dictionary

    //    Dictionary<string, IList<CsvEntry>> csvDictionary = [];
    //    Dictionary<string, int> packageCounter = []; // Used for "pac" number in CSV file.
    //    foreach (string moniker in s_tfmToOpsMoniker.Values)
    //    {
    //        csvDictionary.Add(moniker, []);
    //        packageCounter.Add(moniker, 1);
    //    }

    //    // Get all XML files (ignores disabled indexes).
    //    IEnumerable<string> packageIndexFiles = Directory.EnumerateFiles(indexPackagesPath, "*.xml");
    //    foreach (string packageIndexFile in packageIndexFiles)
    //    {
    //        // Read XML file to get each listed framework.
    //        PackageEntry packageEntry = XmlEntryFormat.ReadPackageEntry(packageIndexFile);

    //        Console.WriteLine($"Creating CSV entries for package {packageEntry.Name}.");

    //        // Add to each applicable CSV file.
    //        foreach (FrameworkEntry frameworkEntry in packageEntry.FrameworkEntries)
    //        {
    //            string framework = frameworkEntry.FrameworkName;
    //            string opsMoniker;
    //            switch (framework)
    //            {
    //                case "net462":
    //                    opsMoniker = s_tfmToOpsMoniker[framework];
    //                    AddCsvEntryToDict(opsMoniker, csvDictionary, packageCounter, packageEntry, frameworkEntry);
    //                    framework = "net47";
    //                    goto case "net47";
    //                case "net47":
    //                    opsMoniker = s_tfmToOpsMoniker[framework];
    //                    AddCsvEntryToDict(opsMoniker, csvDictionary, packageCounter, packageEntry, frameworkEntry);
    //                    framework = "net471";
    //                    goto case "net471";
    //                case "net471":
    //                    opsMoniker = s_tfmToOpsMoniker[framework];
    //                    AddCsvEntryToDict(opsMoniker, csvDictionary, packageCounter, packageEntry, frameworkEntry);
    //                    framework = "net472";
    //                    goto case "net472";
    //                case "net472":
    //                    opsMoniker = s_tfmToOpsMoniker[framework];
    //                    AddCsvEntryToDict(opsMoniker, csvDictionary, packageCounter, packageEntry, frameworkEntry);
    //                    framework = "net48";
    //                    goto case "net48";
    //                case "net48":
    //                    opsMoniker = s_tfmToOpsMoniker[framework];
    //                    AddCsvEntryToDict(opsMoniker, csvDictionary, packageCounter, packageEntry, frameworkEntry);
    //                    framework = "net481";
    //                    goto case "net481";
    //                case "net481":
    //                case "net6.0":
    //                case "net7.0":
    //                case "net8.0":
    //                case "net9.0":
    //                case "netstandard2.0":
    //                case "netstandard2.1":
    //                    opsMoniker = s_tfmToOpsMoniker[framework];
    //                    AddCsvEntryToDict(opsMoniker, csvDictionary, packageCounter, packageEntry, frameworkEntry);
    //                    break;
    //                default:
    //                    Console.WriteLine($"Ignoring target framework {framework}.");
    //                    break;
    //            }
    //        }
    //    }

    //    // Update 08/14: Removed since it caused the pipeline to fail on dependencies.
    //    //// Special case for System.ServiceModel.Primitives - add version 4.10.3.
    //    //// (See https://github.com/dotnet/dotnet-api-docs/pull/10164#discussion_r1696016010.)
    //    //AddCsvEntryToDict("netstandard-2.0", csvDictionary, packageCounter,
    //    //    PackageEntry.Create("System.ServiceModel.Primitives", "4.10.3", "https://github.com/dotnet/wcf", []),
    //    //    FrameworkEntry.Create("netstandard2.0")
    //    //    );

    //    // Create the directory.
    //    Directory.CreateDirectory(csvPath);

    //    foreach (KeyValuePair<string, IList<CsvEntry>> tfm in csvDictionary)
    //    {
    //        // CSV file names must match the folder name in the "binaries" repo:
    //        // e.g. netframework-4.6.2, netstandard-2.0, net-8.0.
    //        string filePath = Path.Combine(csvPath, string.Concat(tfm.Key, ".csv"));

    //        // Delete the file if it already exists.
    //        if (File.Exists(filePath))
    //        {
    //            File.Delete(filePath);
    //        }

    //        using var writer = new StreamWriter(filePath);

    //        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    //        {
    //            // Don't write the header.
    //            HasHeaderRecord = false,
    //        };
    //        using var csv = new CsvWriter(writer, config);
    //        csv.WriteRecords(tfm.Value);
    //    }

    //    static void AddCsvEntryToDict(
    //        string opsMoniker,
    //        Dictionary<string, IList<CsvEntry>> csvDictionary,
    //        Dictionary<string, int> packageCounter,
    //        PackageEntry packageEntry,
    //        FrameworkEntry tfm
    //        )
    //    {
    //        // Special case for packages from dotnet/extensions repo - include XML files.
    //        bool includeXml = string.Equals(
    //            packageEntry.Repository,
    //            "https://github.com/dotnet/extensions",
    //            StringComparison.InvariantCultureIgnoreCase
    //            );

    //        // Except don't include XML file for Microsoft.Extensions.Diagnostics.ResourceMonitoring
    //        // See https://github.com/dotnet/dotnet-api-docs/pull/10395#discussion_r1758128787.
    //        if (string.Equals(
    //            packageEntry.Name,
    //            "Microsoft.Extensions.Diagnostics.ResourceMonitoring",
    //            StringComparison.InvariantCultureIgnoreCase))
    //            includeXml = false;

    //        // Special case for newer assemblies - include XML documentation files.
    //        if (s_packagesWithTruthDocs.Contains(packageEntry.Name))
    //            includeXml = true;

    //        string squareBrackets = $"[tfm={tfm.FrameworkName};includeXml={includeXml}]";

    //        // Special case for System.ServiceModel.Primitives - use reference assemblies.
    //        if (string.Equals(packageEntry.Name, "System.ServiceModel.Primitives", StringComparison.InvariantCultureIgnoreCase))
    //            squareBrackets = $"[tfm={tfm.FrameworkName};includeXml={includeXml};libpath=ref]";

    //        CsvEntry entry = CsvEntry.Create(
    //            string.Concat("pac", packageCounter[opsMoniker]++),
    //            string.Concat(squareBrackets, packageEntry.Name),
    //            packageEntry.Version
    //            );
    //        csvDictionary[opsMoniker].Add(entry);
    //    }
    //}

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
