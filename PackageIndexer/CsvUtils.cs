using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace PackageIndexer;

internal class CsvUtils
{
    static readonly Dictionary<string, string> s_tfmToOpsMoniker = new()
        {
            { "net462", "netframework-4.6.2-pp" },
            { "net47", "netframework-4.7-pp" },
            { "net471", "netframework-4.7.1-pp" },
            { "net472", "netframework-4.7.2-pp" },
            { "net48", "netframework-4.8-pp" },
            { "net481", "netframework-4.8.1-pp" },
            { "net6.0", "net-6.0-pp" },
            { "net7.0", "net-7.0-pp" },
            { "net8.0", "net-8.0-pp" },
            { "net9.0", "net-9.0-pp" },
            { "netstandard2.0", "netstandard-2.0-pp" },
            { "netstandard2.1", "netstandard-2.1-pp" }
        };

    internal static void GenerateCSVFiles(string indexPackagesPath, string csvPath)
    {
        Console.WriteLine("Generating CSV files from package index.");

        // For each package XML file
        //   For each framework
        //     Map it to a known framework name
        //     Generate a collection of that version + later versions of that framework
        //     (e.g. add 4.7, 4.7.1, 4.7.2, 4.8, 4.8.1 for net462; add 7.0, 8.0, 9.0 for net6.0)
        //     Create a dictionary or add to an existing dictionary *for that version* that will become the CSV file -
        //       pac<num>,[tfm=<tfm>;includeXml=false]<package name>,<package version>
        //       Example: pac01,[tfm=net9.0;includeXml=false]Microsoft.Extensions.Caching.Abstractions,9.0.0-preview.2.24128.5
        // Generate a CSV file from each dictionary

        Dictionary<string, IList<CsvEntry>> csvDictionary = [];
        Dictionary<string, int> packageCounter = []; // Used for "pac" number in CSV file.
        foreach (string moniker in s_tfmToOpsMoniker.Values)
        {
            csvDictionary.Add(moniker, []);
            packageCounter.Add(moniker, 1);
        }

        // Get all XML files (ignores disabled indexes).
        IEnumerable<string> packageIndexFiles = Directory.EnumerateFiles(indexPackagesPath, "*.xml");
        foreach (string packageIndexFile in packageIndexFiles)
        {
            // Read XML file to get each listed framework.
            PackageEntry packageEntry = XmlEntryFormat.ReadPackageEntry(packageIndexFile);

            Console.WriteLine($"Creating CSV entries for package {packageEntry.Name}.");

            // Add to each applicable CSV file.
            foreach (FrameworkEntry frameworkEntry in packageEntry.FrameworkEntries)
            {
                string framework = frameworkEntry.FrameworkName;
                string opsMoniker;
                switch (framework)
                {
                    case "net462":
                        framework = "net462";
                        opsMoniker = s_tfmToOpsMoniker[framework];
                        AddCsvEntryToDict(opsMoniker, csvDictionary, packageCounter, packageEntry, framework);
                        goto case "net47";
                    case "net47":
                        framework = "net47";
                        opsMoniker = s_tfmToOpsMoniker[framework];
                        AddCsvEntryToDict(opsMoniker, csvDictionary, packageCounter, packageEntry, framework);
                        goto case "net471";
                    case "net471":
                        framework = "net471";
                        opsMoniker = s_tfmToOpsMoniker[framework];
                        AddCsvEntryToDict(opsMoniker, csvDictionary, packageCounter, packageEntry, framework);
                        goto case "net472";
                    case "net472":
                        framework = "net472";
                        opsMoniker = s_tfmToOpsMoniker[framework];
                        AddCsvEntryToDict(opsMoniker, csvDictionary, packageCounter, packageEntry, framework);
                        goto case "net48";
                    case "net48":
                        framework = "net48";
                        opsMoniker = s_tfmToOpsMoniker[framework];
                        AddCsvEntryToDict(opsMoniker, csvDictionary, packageCounter, packageEntry, framework);
                        goto case "net481";
                    case "net481":
                        framework = "net481";
                        opsMoniker = s_tfmToOpsMoniker[framework];
                        AddCsvEntryToDict(opsMoniker, csvDictionary, packageCounter, packageEntry, framework);
                        break;
                    case "net6.0":
                        framework = "net6.0";
                        opsMoniker = s_tfmToOpsMoniker[framework];
                        AddCsvEntryToDict(opsMoniker, csvDictionary, packageCounter, packageEntry, framework);
                        goto case "net7.0";
                    case "net7.0":
                        framework = "net7.0";
                        opsMoniker = s_tfmToOpsMoniker[framework];
                        AddCsvEntryToDict(opsMoniker, csvDictionary, packageCounter, packageEntry, framework);
                        goto case "net8.0";
                    case "net8.0":
                        framework = "net8.0";
                        opsMoniker = s_tfmToOpsMoniker[framework];
                        AddCsvEntryToDict(opsMoniker, csvDictionary, packageCounter, packageEntry, framework);
                        goto case "net9.0";
                    case "net9.0":
                        framework = "net9.0";
                        opsMoniker = s_tfmToOpsMoniker[framework];
                        AddCsvEntryToDict(opsMoniker, csvDictionary, packageCounter, packageEntry, framework);
                        break;
                    case "netstandard2.0":
                    case "netstandard2.1":
                        opsMoniker = s_tfmToOpsMoniker[framework];
                        AddCsvEntryToDict(opsMoniker, csvDictionary, packageCounter, packageEntry, framework);
                        break;
                    default:
                        Console.WriteLine($"Ignoring target framework {framework}.");
                        break;
                }
            }
        }

        // Create the directory.
        Directory.CreateDirectory(csvPath);

        foreach (KeyValuePair<string, IList<CsvEntry>> tfm in csvDictionary)
        {
            // CSV file names must match the folder name in the "binaries" repo:
            // e.g. netframework-4.6.2-pp, netstandard-2.0-pp, net-8.0-pp.
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
            string[] reposToIncludeXmlComments = [
                "https://github.com/dotnet/extensions",
                "https://devdiv.visualstudio.com/DevDiv/_git/AITestingTools"
                ];

            bool includeXml = reposToIncludeXmlComments.Contains(packageEntry.Repository);

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
