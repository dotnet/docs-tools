using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocateProjects
{
    public static class Logic
    {
        public struct Result
        {
            public const int RETURN_GOOD = 0;
            public const int RETURN_NOPROJ = 1;
            public const int RETURN_TOOMANY = 2;
            public const int RETURN_SLN = 3;

            public int Code;
            public string InputFile;
            public string DiscoveredFile;

            public Result(int code, string inputFile, string discoveredFile)
            {
                Code = code;
                InputFile = inputFile;
                DiscoveredFile = discoveredFile;
            }
        }

        public static void Test(string _rootDir, string item, string[] includeExtensions, out Result? resultValue)
        {
            Test(_rootDir, item, includeExtensions, out string? textResult);

            if (textResult == null) resultValue = null;
            else
            {
                string[] parts = textResult.Split('|');
                resultValue = new Result(int.Parse(parts[0]), parts[1], parts[2]);
            }
        }

        public static void Test(string _rootDir, string item, string[] includeExtensions, out string? resultValue)
        {
            resultValue = null;
            Directory.SetCurrentDirectory(_rootDir);
            // Extensions variable does not include two special files we do care about.
            // Check them here:
            var file = Path.GetFileName(item);
            bool specialJsonFile = (file == "snippets.5000.json") || (file == "global.json");
            if (!specialJsonFile &&
                ((includeExtensions.Length != 0 && !includeExtensions.Contains(Path.GetExtension(item), System.StringComparer.OrdinalIgnoreCase))))
                return;

            var folders = item.Split("/");

            // Deleted files require special checking
            // - When a code file is deleted and 1 project/sln file is found in the folder or parent folder: PROCESS
            // - When a code file is deleted and no project/sln file is found in the folder or parent folder: SKIP
            // - If a project/sln is deleted and code files remain at or below the current folder and no project/sln file is found above or below: ERROR
            // - If a project/sln is deleted and no code files remain at or below and no project/sln file is found above or below: SKIP
            bool deletedFile = !File.Exists(item);
            int returnCode = Result.RETURN_NOPROJ;
            string returnFile = item;
            string returnProj = "";
            bool checkingSln = Path.GetExtension(item) == ".sln";
            bool checkingProj = Path.GetExtension(item).Contains("proj");
            // Well, this is ugly:
            bool moreCodeExists = Directory.Exists(Path.GetDirectoryName(item)) &&
                Directory.EnumerateFiles(Path.GetDirectoryName(item)!, "*.cs", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(Path.GetDirectoryName(item)!, "*.vb", SearchOption.AllDirectories))
                .Concat(Directory.EnumerateFiles(Path.GetDirectoryName(item)!, "*.fs", SearchOption.AllDirectories))
                .Concat(Directory.EnumerateFiles(Path.GetDirectoryName(item)!, "*.cpp", SearchOption.AllDirectories))
                .Concat(Directory.EnumerateFiles(Path.GetDirectoryName(item)!, "*.h", SearchOption.AllDirectories))
                .Concat(Directory.EnumerateFiles(Path.GetDirectoryName(item)!, "*.xaml", SearchOption.AllDirectories))
                .Concat(Directory.EnumerateFiles(Path.GetDirectoryName(item)!, "*.razor", SearchOption.AllDirectories))
                .Concat(Directory.EnumerateFiles(Path.GetDirectoryName(item)!, "*.cshtml", SearchOption.AllDirectories))
                .Concat(Directory.EnumerateFiles(Path.GetDirectoryName(item)!, "*.vbhtml", SearchOption.AllDirectories))
                .Any();

            var subPath = ".";
            // The important part of this logic is that for any source file that
            // is part of this PR, it must be part of exactly one project. That one
            // project may be part of a multi-project solution.
            foreach (var folder in folders[0..^1]) // Don't include the file name component.
            {
                // We already have too many, no need to keep checking
                if (returnCode == Result.RETURN_TOOMANY)
                    break;

                subPath = $"{subPath}/{folder}";
                if (!Directory.Exists(folder))
                {
                    break;
                }

                Directory.SetCurrentDirectory(folder);

                // Local function to set return values
                void LoopFiles(IEnumerable<string> fileCollection, bool isFindSLN)
                {
                    // We already have too many, no need to keep checking
                    if (returnCode == Result.RETURN_TOOMANY)
                        return;

                    foreach (var file in fileCollection)
                    {
                        // Never found a proj/sln until now
                        if (returnCode == Result.RETURN_NOPROJ)
                        {
                            returnCode = isFindSLN ? Result.RETURN_SLN : Result.RETURN_GOOD;
                            returnProj = $"{subPath}/{Path.GetFileName(file)}";
                        }
                        // Found a solution earlier, we can find 1 project, no more
                        else if (!isFindSLN && returnCode == Result.RETURN_SLN)
                        {
                            returnCode = Result.RETURN_GOOD;
                        }
                        // We already found something, but we found another
                        else if (returnCode == Result.RETURN_GOOD)
                        {
                            returnCode = Result.RETURN_TOOMANY;
                            break;
                        }
                    }
                }

                LoopFiles(Directory.EnumerateFiles(".", "*.sln", SearchOption.TopDirectoryOnly), true);
                LoopFiles(Directory.EnumerateFiles(".", "*.csproj", SearchOption.TopDirectoryOnly), false);
                LoopFiles(Directory.EnumerateFiles(".", "*.vbproj", SearchOption.TopDirectoryOnly), false);
                LoopFiles(Directory.EnumerateFiles(".", "*.fsproj", SearchOption.TopDirectoryOnly), false);
                LoopFiles(Directory.EnumerateFiles(".", "*.vcxproj", SearchOption.TopDirectoryOnly), false);
            }

            // If we're actually checking a sln (it was modified/added) we don't want to error
            if ((checkingSln || specialJsonFile) && returnCode == Result.RETURN_SLN)
                returnCode = Result.RETURN_GOOD;

            if (deletedFile && (returnCode == Result.RETURN_NOPROJ) && !moreCodeExists)
            {
                // all code gone. It's OK
                return;
            }

            // This works for code files that are deleted.
            if ((deletedFile) && !(checkingProj || checkingSln) && (returnCode == Result.RETURN_NOPROJ))
                return;

            resultValue = $"{returnCode}|{returnFile}|{returnProj}";
        }
    }
}
