using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace PackageIndexer;

internal class CsvUtils
{
    static readonly Dictionary<string, string> s_targetFrameworks = new()
    {
        { "8.", "net8.0" }, // Latest stable.
        { "9.", "net9.0" } // Latest preview.
    };

    static readonly Dictionary<string, string> s_opsMonikers = new()
    {
        { "8.", "dotnet-plat-ext-8.0" }, // Latest stable.
        { "9.", "dotnet-plat-ext-9.0" } // Latest preview.
    };

    internal static void GenerateCSVFiles(string indexPackagesPath, string csvPath)
    {
        Console.WriteLine("Generating CSV files from package index.");

        // For each package XML file
        //   For each framework
        //     Map it to a known framework name
        //     Create a dictionary or add to an existing dictionary *for that version* that will become the CSV file -
        //       pac<num>,[tfm=<tfm>;includeXml=false]<package name>,<package version>
        //       Example: pac01,[tfm=net9.0;includeXml=false]Microsoft.Extensions.Caching.Abstractions,9.0.0-preview.2.24128.5
        // Generate a CSV file from each dictionary

        Dictionary<string, IList<CsvEntry>> csvDictionary = [];
        Dictionary<string, int> packageCounter = []; // Used for "pac" number in CSV file.
        foreach (string moniker in s_opsMonikers.Values)
        {
            csvDictionary.Add(moniker, []);
            packageCounter.Add(moniker, 1);
        }

        // Get all XML files (ignores disabled indexes).
        IEnumerable<string> packageIndexFiles = Directory.EnumerateFiles(indexPackagesPath, "*.xml");
        foreach (string packageIndexFile in packageIndexFiles)
        {
            PackageEntry packageEntry = XmlEntryFormat.ReadPackageEntry(packageIndexFile);

            Console.WriteLine($"Creating CSV entries for package {packageEntry.Name}.");

            // Get OPS moniker and TFM from the package version.
            string shortVersion = packageEntry.Version.Substring(0, 2);
            if (!s_targetFrameworks.TryGetValue(shortVersion, out string tfm))
            {
                Console.WriteLine($"Ignoring package {packageEntry.Name} {packageEntry.Version}.");
                continue;
            }
            string opsMoniker = s_opsMonikers[shortVersion];

            AddCsvEntryToDict(opsMoniker, csvDictionary, packageCounter, packageEntry, tfm);
        }

        // Create the directory.
        Directory.CreateDirectory(csvPath);

        foreach (KeyValuePair<string, IList<CsvEntry>> tfm in csvDictionary)
        {
            // CSV file names must match the folder name in the "binaries" repo:
            // e.g. netframework-4.6.2, netstandard-2.0, net-8.0.
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

        static void AddCsvEntryToDict(
            string opsMoniker,
            Dictionary<string, IList<CsvEntry>> csvDictionary,
            Dictionary<string, int> packageCounter,
            PackageEntry packageEntry,
            string frameworkName
            )
        {
            // Special case for packages from dotnet/extensions repo - include XML files.
            bool includeXml = string.Equals(
                packageEntry.Repository,
                "https://github.com/dotnet/extensions",
                StringComparison.InvariantCultureIgnoreCase
                );

            // Except don't include XML file for Microsoft.Extensions.Diagnostics.ResourceMonitoring
            // See https://github.com/dotnet/dotnet-api-docs/pull/10395#discussion_r1758128787.
            if (string.Equals(
                packageEntry.Name,
                "Microsoft.Extensions.Diagnostics.ResourceMonitoring",
                StringComparison.InvariantCultureIgnoreCase))
                includeXml = false;

            // Special case for newer assemblies - include XML documentation files.
            if (PlatformPackageDefinition.PackagesWithTruthDocs.Contains(packageEntry.Name))
                includeXml = true;

            string squareBrackets = $"[tfm={frameworkName};includeXml={includeXml}]";

            // Special case for System.ServiceModel.Primitives - use reference assemblies.
            if (string.Equals(packageEntry.Name, "System.ServiceModel.Primitives", StringComparison.InvariantCultureIgnoreCase))
                squareBrackets = $"[tfm={frameworkName};includeXml={includeXml};libpath=ref]";

            CsvEntry entry = CsvEntry.Create(
                string.Concat("pac", packageCounter[opsMoniker]++),
                string.Concat(squareBrackets, packageEntry.Name),
                packageEntry.Version
                );
            csvDictionary[opsMoniker].Add(entry);
        }
    }
}
