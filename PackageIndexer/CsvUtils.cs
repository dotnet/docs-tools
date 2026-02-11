using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace PackageIndexer;

internal class CsvUtils
{
    Dictionary<string, IList<CsvEntry>> _csvDictionary = [];
    Dictionary<string, int> _packageCounter = []; // Used for "pac" number in CSV file.

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
            { "net10.0", "net-10.0-pp" },
            { "net11.0", "net-11.0-pp" },
            { "netstandard2.0", "netstandard-2.0-pp" },
            { "netstandard2.1", "netstandard-2.1-pp" }
        };

    internal CsvUtils()
    {
        // Initialize the two dictionaries.
        foreach (string moniker in s_tfmToOpsMoniker.Values)
        {
            _csvDictionary.Add(moniker, []);
            _packageCounter.Add(moniker, 1);
        }
    }

    internal void GenerateCSVFiles(string indexPackagesPath, string csvPath)
    {
        Console.WriteLine("Generating CSV files from package index.");

        // For each package XML file
        //   For each framework
        //     Map it to a known framework name
        //     Generate a collection of that version + compatible versions (e.g. also add to net9.0 moniker for net8.0 assets,
        //     *if* net9.0 isn't already explicitly targeted).
        //     Create a dictionary or add to an existing dictionary *for that version* that will become the CSV file -
        //       pac<num>,[tfm=<tfm>;includeXml=false]<package name>,<package version>
        //       Example: pac01,[tfm=net9.0;includeXml=false]Microsoft.Extensions.Caching.Abstractions,9.0.0-preview.2.24128.5
        // Generate a CSV file from each dictionary

        // Get all XML files (ignores disabled indexes).
        IEnumerable<string> packageIndexFiles = Directory.EnumerateFiles(indexPackagesPath, "*.xml");
        foreach (string packageIndexFile in packageIndexFiles)
        {
            // Read XML file to get each listed framework.
            PackageEntry packageEntry = XmlEntryFormat.ReadPackageEntry(packageIndexFile);

            // These packages cause an error in the pipeline build
            // when included in the netframework CSV files.
            string[] toExcludeFromFramework = ["System.Data.Odbc", "System.Data.OleDb", "System.Data.SqlClient", "System.IO.Ports"];

            Console.WriteLine($"Creating CSV entries for package {packageEntry.Name}.");

            // Add to each applicable CSV file.
            foreach (string targetFramework in packageEntry.Frameworks)
            {
                string opsMoniker;
                string? fellThroughFromVersion = null;
                switch (targetFramework)
                {
                    case "net6.0":
                        opsMoniker = s_tfmToOpsMoniker["net6.0"];
                        AddCsvEntryToDict(opsMoniker, packageEntry, "net6.0");
                        if (!packageEntry.Frameworks.Contains("net7.0"))
                        {
                            // Add to net7.0 moniker since this is a compatible framework.
                            fellThroughFromVersion = "net6.0";
                            goto case "net7.0";
                        }
                        break;
                    case "net7.0":
                        opsMoniker = s_tfmToOpsMoniker["net7.0"];
                        AddCsvEntryToDict(opsMoniker, packageEntry, fellThroughFromVersion ?? "net7.0");
                        if (!packageEntry.Frameworks.Contains("net8.0"))
                        {
                            // Add to net8.0 moniker since this is a compatible framework.
                            fellThroughFromVersion = fellThroughFromVersion ?? "net7.0";
                            goto case "net8.0";
                        }
                        break;
                    case "net8.0":
                        opsMoniker = s_tfmToOpsMoniker["net8.0"];
                        AddCsvEntryToDict(opsMoniker, packageEntry, fellThroughFromVersion ?? "net8.0");
                        if (!packageEntry.Frameworks.Contains("net9.0"))
                        {
                            // Add to net9.0 moniker since this is a compatible framework.
                            fellThroughFromVersion = fellThroughFromVersion ?? "net8.0";
                            goto case "net9.0";
                        }
                        break;
                    case "net9.0":
                        opsMoniker = s_tfmToOpsMoniker["net9.0"];
                        AddCsvEntryToDict(opsMoniker, packageEntry, fellThroughFromVersion ?? "net9.0");
                        if (!packageEntry.Frameworks.Contains("net10.0"))
                        {
                            // Add to net10.0 moniker since this is a compatible framework.
                            fellThroughFromVersion = fellThroughFromVersion ?? "net9.0";
                            goto case "net10.0";
                        }
                        break;
                    case "net10.0":
                        opsMoniker = s_tfmToOpsMoniker["net10.0"];
                        AddCsvEntryToDict(opsMoniker, packageEntry, fellThroughFromVersion ?? "net10.0");
                        if (!packageEntry.Frameworks.Contains("net11.0"))
                        {
                            // Add to net11.0 moniker since this is a compatible framework.
                            fellThroughFromVersion = fellThroughFromVersion ?? "net10.0";
                            goto case "net11.0";
                        }
                        break;
                    case "net11.0":
                        opsMoniker = s_tfmToOpsMoniker["net11.0"];
                        AddCsvEntryToDict(opsMoniker, packageEntry, fellThroughFromVersion ?? "net11.0");
                        break;
                    case "net462":
                        opsMoniker = s_tfmToOpsMoniker["net462"];

                        if (toExcludeFromFramework.Contains(packageEntry.Name, StringComparer.InvariantCultureIgnoreCase))
                        {
                            Console.WriteLine($"Excluding {packageEntry.Name} from {opsMoniker} CSV.");
                            break;
                        }

                        AddCsvEntryToDict(opsMoniker, packageEntry, "net462");
                        if (!packageEntry.Frameworks.Contains("net47"))
                        {
                            // Add to net47 moniker since this is a compatible framework.
                            fellThroughFromVersion = "net462";
                            goto case "net47";
                        }
                        break;
                    case "net47":
                        opsMoniker = s_tfmToOpsMoniker["net47"];

                        if (toExcludeFromFramework.Contains(packageEntry.Name, StringComparer.InvariantCultureIgnoreCase))
                        {
                            Console.WriteLine($"Excluding {packageEntry.Name} from {opsMoniker} CSV.");
                            break;
                        }

                        AddCsvEntryToDict(opsMoniker, packageEntry, fellThroughFromVersion ?? "net47");
                        if (!packageEntry.Frameworks.Contains("net471"))
                        {
                            // Add to net471 moniker since this is a compatible framework.
                            fellThroughFromVersion ??= "net47";
                            goto case "net471";
                        }
                        break;
                    case "net471":
                        opsMoniker = s_tfmToOpsMoniker["net471"];

                        if (toExcludeFromFramework.Contains(packageEntry.Name, StringComparer.InvariantCultureIgnoreCase))
                        {
                            Console.WriteLine($"Excluding {packageEntry.Name} from {opsMoniker} CSV.");
                            break;
                        }

                        AddCsvEntryToDict(opsMoniker, packageEntry, fellThroughFromVersion ?? "net471");
                        if (!packageEntry.Frameworks.Contains("net472"))
                        {
                            // Add to net472 moniker since this is a compatible framework.
                            fellThroughFromVersion ??= "net471";
                            goto case "net472";
                        }
                        break;
                    case "net472":
                        opsMoniker = s_tfmToOpsMoniker["net472"];

                        if (toExcludeFromFramework.Contains(packageEntry.Name, StringComparer.InvariantCultureIgnoreCase))
                        {
                            Console.WriteLine($"Excluding {packageEntry.Name} from {opsMoniker} CSV.");
                            break;
                        }

                        AddCsvEntryToDict(opsMoniker, packageEntry, fellThroughFromVersion ?? "net472");
                        if (!packageEntry.Frameworks.Contains("net48"))
                        {
                            // Add to net48 moniker since this is a compatible framework.
                            fellThroughFromVersion ??= "net472";
                            goto case "net48";
                        }
                        break;
                    case "net48":
                        opsMoniker = s_tfmToOpsMoniker["net48"];

                        if (toExcludeFromFramework.Contains(packageEntry.Name, StringComparer.InvariantCultureIgnoreCase))
                        {
                            Console.WriteLine($"Excluding {packageEntry.Name} from {opsMoniker} CSV.");
                            break;
                        }

                        AddCsvEntryToDict(opsMoniker, packageEntry, fellThroughFromVersion ?? "net48");
                        if (!packageEntry.Frameworks.Contains("net481"))
                        {
                            // Add to net481 moniker since this is a compatible framework.
                            fellThroughFromVersion ??= "net48";
                            goto case "net481";
                        }
                        break;
                    case "net481":
                        opsMoniker = s_tfmToOpsMoniker["net481"];

                        if (toExcludeFromFramework.Contains(packageEntry.Name, StringComparer.InvariantCultureIgnoreCase))
                        {
                            Console.WriteLine($"Excluding {packageEntry.Name} from {opsMoniker} CSV.");
                            break;
                        }

                        AddCsvEntryToDict(opsMoniker, packageEntry, fellThroughFromVersion ?? "net481");
                        break;
                    case "netstandard2.0":
                    case "netstandard2.1":
                        opsMoniker = s_tfmToOpsMoniker[targetFramework];
                        AddCsvEntryToDict(opsMoniker, packageEntry, targetFramework);
                        break;
                    default:
                        Console.WriteLine($"Ignoring target framework {targetFramework}.");
                        break;
                }
            }
        }

        // Create the directory.
        Directory.CreateDirectory(csvPath);

        foreach (KeyValuePair<string, IList<CsvEntry>> tfm in _csvDictionary)
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

        void AddCsvEntryToDict(
            string opsMoniker,
            PackageEntry packageEntry,
            string targetFramework
            )
        {
            bool includeXml = true;

            if (PlatformPackageDefinition.runtimePackagesWithoutDocs.Contains(packageEntry.Name) ||
                PlatformPackageDefinition.otherPackagesWithoutDocs.Contains(packageEntry.Name))
                includeXml = false;

            // And don't include XMl files for Microsoft.Bcl.* packages.
            if (packageEntry.Name.StartsWith("Microsoft.Bcl.", StringComparison.InvariantCultureIgnoreCase))
                includeXml = false;

            string squareBrackets = $"[tfm={targetFramework};includeXml={includeXml}]";

            // Special case for System.ServiceModel.Primitives - use reference assemblies.
            if (string.Equals(packageEntry.Name, "System.ServiceModel.Primitives", StringComparison.InvariantCultureIgnoreCase))
                squareBrackets = $"[tfm={targetFramework};includeXml={includeXml};libpath=ref]";

            CsvEntry entry = CsvEntry.Create(
                string.Concat("pac", _packageCounter[opsMoniker]++),
                string.Concat(squareBrackets, packageEntry.Name),
                packageEntry.Version
                );

            _csvDictionary[opsMoniker].Add(entry);
        }
    }

    internal static void CopyCSVFiles(string csvPath, string ciSourceRepoPath)
    {
        Console.WriteLine("Copying CSV files to CI source repo.");
        // Get all CSV files.
        IEnumerable<string> csvFiles = Directory.EnumerateFiles(csvPath, "*.csv");
        foreach (string csvFile in csvFiles)
        {
            string fileName = Path.GetFileName(csvFile);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(csvFile);

            string destDirectoryPath = Path.Combine(ciSourceRepoPath.ToString()!, fileNameWithoutExtension);
            string destFilePath = Path.Combine(destDirectoryPath, fileName);

            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirectoryPath))
            {
                Directory.CreateDirectory(destDirectoryPath);
            }

            // Copy the file.
            File.Copy(csvFile, destFilePath, true);
        }
    }
}
