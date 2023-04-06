using DotNetDocs.Tools.Utility;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("PullRequestSimulations")]

namespace LocateProjects;

// Notes on turning this spike into a fully functioning tool for our 
// build system.
// 
// Current status:
//      This spike finds all *.sln files under a given root directory.
//      In addition, it finds all *.*proj files that are not in the
//      same folder or a child folder of a *.sln file. That should
//      be the full list of potential projects to build in any given
//      configuration.
//
// Requirements:  
//      This program should build a list of all projects / solutions
//      to build under different conditions and configurations:
//      - One option is to switch on a full build of the main
//          branch, or to build those projects affected by a PR.
//      - Another option should select which environment to build
//          projects for. Possibilities are:
//          - .NET Core on unix
//          - .NET Core on Windows (superset of above, includes desktop
//          projects)
//          - .NET Framework projects (new style), Windows only
//          - .NET Framework old style projects, Windows only.
//      Open question: Can this tool detect the OS and make that determination?
//      Or should that be a project switch? Tentative answer: OS can be detected
//      by this application. If "unix", (from Environment.OSVersion.Platform),
//      Any of the Windows only projects would not be returned.
//
// Assumptions:
//      - We'll spin up different containers for each of these configurations:
//          o .NET Core on unix
//          o .NET Core on Windows
//          o .NET Framework on Windows
//      This assumption means that the list of projects is never "everything to
//      build on all configs and platforms".
// 
// Proposed command line:
//  LocateProjects <rootdir> -f|--framework -p|--pullrequest <ID>
//      <rootdir>: Required. The root directory of the cloned repo.
//      -f|--framework: build framework style projects (valid on Windows only).
//          If this option is not specified, .NET Core projects are built.
//      -p|--pr <ID>: The Pull request to build. If this option is 
//          not specified, the main branch is built.
//          Note that a main branch buid builds *all* applicable
//          projects. A PR build only builds affected projects on
//          the current configuration. PR should be a number.
//
// Note that this implies the possibility that a container is created
// where nothing needs to be built. Consider a PR with a .NET Core 
// desktop WPF application. The .NET Core linux container should not
// build anything, and the .NET Framework windows container would not
// build anything.
//
// Next set of tasks:
// 0. Remove hard coded dotnet/samples as the repo. (Make it configured).
// 1. algorithms and unit tests on determining project and solution types.
// 2. algorithms and unit tests on projects for target framework(s).
// 3. Update output to match command line and OS switches.


// Error codes per file:
//   0 - No error
//   1 - No project/solution file found
//   2 - More than one project/solution file found


class Program
{
    const string OUTPUT_ERROR_1_NOPROJ = "ERROR: Project missing. A project (and optionally a solution file) must be in this directory or one of the parent directories to validate and build this code.";
    const string OUTPUT_ERROR_2_TOOMANY = "ERROR: Too many projects found. A single project or solution must exist in this directory or one of the parent directories.";
    const string OUTPUT_ERROR_3_SLNNOPROJ = "ERROR: Solution found, but missing project. A project is required to compile this code.";
    const string OUTPUT_GOOD = "GOOD: Passed structural tests.";
    const string SNIPPETS_FILE_NAME = "snippets.5000.json";

    const int EXITCODE_GOOD = 0;
    const int EXITCODE_BAD = 1;

    const string FANCY_BATCH_FILENAME = "snippets5000_runner.bat";

    /// <summary>
    /// LocateProjects: Find all projects and solutions requiring a build.
    /// </summary>
    /// <param name="sourcepath">The directory containing the local source tree.</param>
    /// <param name="pullrequest">If available, the number of the pull request being built.</param>
    /// <param name="owner">If available, the owner organization of the repository.</param>
    /// <param name="repo">If available, the name of the repository.</param>
    /// <returns>0 on success. Otherwise, a non-zero error code.</returns>
    /// <remarks>
    /// The output from standard out is the list of all projects and 
    /// solutions that should be built. If nothing but the rootdir is specified,
    /// it will output all solutions, and all projects that are not part of a solution.
    /// </remarks>
    static async Task<int> Main(string sourcepath, int? pullrequest = default, string? owner=default, string? repo=default, string? dryrunTestId=default, string? dryrunTestDateFile=default)
    {
        int exitCode = EXITCODE_GOOD;

        if ((pullrequest.HasValue) &&
            !string.IsNullOrEmpty(owner) &&
            !string.IsNullOrEmpty(repo))
        {
            IEnumerable<DiscoveryResult> projects;

            // Normal github PR
            if (string.IsNullOrEmpty(dryrunTestId))
            {
                var key = CommandLineUtility.GetEnvVariable("GitHubKey", "You must store your GitHub key in the 'GitHubKey' environment variable", null);

                List<DiscoveryResult> localResults = new();
                await foreach (var item in new PullRequestProjectList(owner, repo, pullrequest.Value, sourcepath).GenerateBuildList(key))
                    localResults.Add(item);

                projects = localResults;
            }

            // NOT a normal github PR and instead is a test
            else if (string.IsNullOrEmpty(dryrunTestDateFile))
                throw new ArgumentNullException("The dryrun Test DataFile must be set");
            else
                projects = new TestingProjectList(dryrunTestId, dryrunTestDateFile, sourcepath).GenerateBuildList();

            Console.WriteLine("\r\nOutput all items found, grouped by status...");

            // Start processing all of the discovered projects
            List<SnippetsConfigFile> transformedProjects;
            string[] projectsToCompile;
            ProcessDiscoveredProjects(projects, out transformedProjects, out projectsToCompile);

            // Compile each project
            exitCode = await CompileProjects(sourcepath, exitCode, transformedProjects, projectsToCompile);

            // Start hunting for errors
            ProcessFailedProjects(repo, transformedProjects.Where(p => p.RunExitCode != 0));

            // Final results
            bool first = false;
            foreach (var item in transformedProjects.Where(p => !p.RunConsideredGood))
            {
                if (!first)
                {
                    Console.WriteLine($"\r\n😭 Compile targets with unresolved issues:");
                    first = true;
                }
                Console.WriteLine($"{Log(2)}{item.RunTargetFile}");
                exitCode = EXITCODE_BAD;
            }

            if (exitCode == 0)
                Console.WriteLine($"\r\n😀 All builds passing! 😀");

            return exitCode;
        }
        // TODO
        else
        {
            var fullBuild = new FullBuildProjectList(sourcepath);
            foreach (var path in fullBuild.GenerateBuildList())
                Console.WriteLine(path);
        }
        return 0;
    }

    private static void ProcessDiscoveredProjects(IEnumerable<DiscoveryResult> projects, out List<SnippetsConfigFile> transformedProjects, out string[] projectsToCompile)
    {
        // Results collection
        transformedProjects = new List<SnippetsConfigFile>();

        // ERROR no project
        bool first = false;
        foreach (var project in projects.Where(p => p.Code == DiscoveryResult.RETURN_NOPROJ))
        {
            if (!first) { Console.WriteLine(OUTPUT_ERROR_1_NOPROJ); first = true; }
            Console.WriteLine($"::error file={project.InputFile},line=0,col=0::{OUTPUT_ERROR_1_NOPROJ}");
            transformedProjects.Add(new SnippetsConfigFile() { RunOutput = OUTPUT_ERROR_1_NOPROJ, RunExitCode = project.Code, RunTargetFile = project.InputFile, RunErrorIsStructural = true, RunConsideredGood = false });
        }

        // ERROR too many projects
        first = false;
        foreach (var project in projects.Where(p => p.Code == DiscoveryResult.RETURN_TOOMANY))
        {
            if (!first) { Console.WriteLine(OUTPUT_ERROR_2_TOOMANY); first = true; }
            Console.WriteLine($"::error file={project.InputFile},line=0,col=0::{OUTPUT_ERROR_2_TOOMANY}");
            transformedProjects.Add(new SnippetsConfigFile() { RunOutput = OUTPUT_ERROR_2_TOOMANY, RunExitCode = project.Code, RunTargetFile = project.InputFile, RunErrorIsStructural = true, RunConsideredGood = false });
        }

        // TODO: I don't think we want this scenario
        // ERROR solution but no proj
        first = false;
        foreach (var project in projects.Where(p => p.Code == DiscoveryResult.RETURN_SLN))
        {
            if (!first) { Console.WriteLine(OUTPUT_ERROR_3_SLNNOPROJ); first = true; }
            Console.WriteLine($"::error file={project.InputFile},line=0,col=0::{OUTPUT_ERROR_3_SLNNOPROJ}");
            transformedProjects.Add(new SnippetsConfigFile() { RunOutput = OUTPUT_ERROR_3_SLNNOPROJ, RunExitCode = project.Code, RunTargetFile = project.InputFile, RunErrorIsStructural = true, RunConsideredGood = false });
        }

        // NO ERROR output each item
        first = false;
        foreach (var project in projects.Where(p => p.Code == DiscoveryResult.RETURN_GOOD))
        {
            if (!first) { Console.WriteLine(OUTPUT_GOOD); first = true; }
            Console.WriteLine(project);
        }

        Console.WriteLine("\r\nGathering unique projects to compile:");
        projectsToCompile = projects.Where(p => p.Code == DiscoveryResult.RETURN_GOOD).Select(p => p.DiscoveredFile).Distinct().ToArray();

        // Gather the files to be tested:
        foreach (var item in projectsToCompile)
            Console.WriteLine($"  {item}");

        Console.WriteLine("\r\nCompile projects...");
    }

    private static async Task<int> CompileProjects(string sourcePath, int exitCode, List<SnippetsConfigFile> transformedProjects, string[] projectsToCompile)
    {
        // The variables from the code put into a dictionary. Can be used with the custom
        // command line. Emulates the PowerShell ExpandString system.
        Dictionary<string, string> expansionVariables = new Dictionary<string, string>(3);

        string visualStudioBatchFile = CommandLineUtility.GetEnvVariable("VS_DEVCMD", "", "C:\\Program Files\\Microsoft Visual Studio\\2022\\Enterprise\\Common7\\Tools\\VsDevCmd.bat");

        int counter = 1;
        foreach (var item in projectsToCompile)
        {
            expansionVariables.Clear();

            Directory.SetCurrentDirectory(sourcePath);
            Console.WriteLine("\r\n===================================");
            Console.WriteLine($"Compile: {counter}/{projectsToCompile.Length} {item}");

            string projectPath = Path.GetFullPath(item);
            Console.WriteLine($"{Log(2)}Resolved path: {projectPath}");

            expansionVariables.Add("repoRoot", sourcePath);
            expansionVariables.Add("projectPath", projectPath);
            expansionVariables.Add("projectDirectory", Path.GetDirectoryName(projectPath)!);

            SnippetsConfigFile config = new();

            // Check if snippets config file exists
            string possibleSnippetsFilePath = Path.Combine(Path.GetDirectoryName(projectPath)!, SNIPPETS_FILE_NAME);
            if (File.Exists(possibleSnippetsFilePath))
            {
                Console.WriteLine($"{Log(2)}Found snippets config file");

                try
                {
                    config = SnippetsConfigFile.Load(possibleSnippetsFilePath);
                }
                catch (Exception e1)
                {
                    Console.WriteLine($"{Log(2)}Unable to load config file: {e1.StackTrace}");
                }
            }

            Console.WriteLine($"{Log(2)}Mode: {config.Host}");

            Directory.SetCurrentDirectory(Path.GetDirectoryName(projectPath)!);

            // Build the batch file that is run for this project.

            if (config.Host == "dotnet")
            {
                await File.WriteAllTextAsync(FANCY_BATCH_FILENAME, $"dotnet build \"{projectPath}\"");
            }
            else if (config.Host == "visualstudio")
            {
                

                string batchFileContent =
                    $"CALL \"{visualStudioBatchFile}\"\r\n" +
                    $"nuget.exe restore \"{projectPath}\"\r\n" +
                    $"msbuild.exe \"{projectPath}\" -restore:True\r\n";

                await File.WriteAllTextAsync(FANCY_BATCH_FILENAME, batchFileContent);
            }
            else if (config.Host == "custom")
            {
                if (config.Command is not null)
                {
                    foreach (var key in expansionVariables.Keys)
                        config.Command = config.Command.Replace($"{{{key}}}", expansionVariables[key]);

                    await File.WriteAllTextAsync(FANCY_BATCH_FILENAME, $"dotnet build \"{projectPath}\"");
                }
                else
                {
                    Console.WriteLine($"{Log(2)}Mode is custom but command isn't set");
                    config.RunOutput = "Invalid snippets file, missing command for custom action";
                    config.RunConsideredGood = false;
                    exitCode = EXITCODE_BAD;
                }
            }
            else
            {
                Console.WriteLine($"{Log(2)}Mode is invalid... nothing to do");
                config.RunConsideredGood = false;
                exitCode = EXITCODE_BAD;
            }

            config.RunTargetFile = projectPath;

            // Run the batch file to do the compile.
            if (config.RunConsideredGood)
            {
                Console.WriteLine($"{Log(2)}Contents of {FANCY_BATCH_FILENAME}:");
                foreach (var line in File.ReadAllLines(FANCY_BATCH_FILENAME))
                    Console.WriteLine($"{Log(4)}{line}");

                ProcessStartInfo processInfo = new ProcessStartInfo(FANCY_BATCH_FILENAME)
                {
                    WorkingDirectory = Path.GetDirectoryName(projectPath),
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                };

                Process process = new Process()
                {
                    StartInfo = processInfo,

                };
                process.ErrorDataReceived += Process_ErrorDataReceived;
                process.OutputDataReceived += Process_ErrorDataReceived;

                // Capture the results of the output
                void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e) =>
                    config.RunOutput += $"{e.Data}\r\n";

                // Start the process
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();

                // Capture exit code and log output
                config.RunExitCode = process.ExitCode;
                config.RunConsideredGood = config.RunExitCode == 0;

                config.RunOutput = config.RunOutput.Trim();
                Console.WriteLine($"{Log(2)}Output: \r\n{Log(4)}{config.RunOutput.Replace("\n", $"\n{Log(4)}")}\r\n");
            }
            transformedProjects.Add(config);

            counter++;
        }

        Console.WriteLine();
        return exitCode;
    }

    private static void ProcessFailedProjects(string repo, IEnumerable<SnippetsConfigFile> failedProjects)
    {
        bool first = false;

        // Process all of the results and ignore any known errors
        foreach (var config in failedProjects)
        {
            if (!first)
            {
                Console.WriteLine($"\r\nSome projects failed to compile...");
                first = true;
            }

            List<(string ErrorCode, string ErrorLine)> everyError = new();
            Console.WriteLine($"\r\nProcessing failure: {config.RunTargetFile}");

            foreach (var line in config.RunOutput.Split('\n'))
            {
                Match match = Regex.Match(line.Trim(), ": (?:Solution file error|error) ([^:]*)");

                if (match.Success)
                    everyError.Add((match.Groups[1].Value, line.Trim()));
            }

            if (everyError.Count == 0)
            {
                /*
                 * This code commented out for now. We've not enabled the ability to bypass structural errors
                 * if we do, this code will enable that with a little more work
                 * 

                // If this error is from the project system not finding something, alter the reporting data to see if it's excluded
                if (config.RunErrorIsStructural)
                {
                    Console.WriteLine($"{Log(2)}Code:{config.RunExitCode} Reason:{config.RunOutput}");

                    // TODO: We should scan the current folder of the target and any folder above it to
                    //       find a snippets config file and see if the error is reported in it that matches.

                    // Skippable error, only need to find one. LINQ is more expressive then a foreach + if check
                    foreach (var _ in from error in config.ExpectedErrors
                                      where error.Error == config.RunExitCode.ToString()
                                            && config.RunTargetFile!.EndsWith(config.ExpectedErrors[0].File, StringComparison.OrdinalIgnoreCase)
                                            && error.Line == 0
                                      select new { })
                    {
                        Console.WriteLine($"{Log(2)}Skipping this error");
                        config.RunConsideredGood = true;
                        break;
                    }
                }
                else
                */
                
                Console.WriteLine($"{Log(2)}Unable to find error from output");
            }

            // Normal MSBUILD errors
            else
            {
                everyError = everyError.Distinct().ToList();

                int errorsSkipped = 0;
                foreach (var (errorCode, errorLine) in everyError)
                {
                    Console.WriteLine($"\r\n{Log(2)}Found error code: {errorCode} on line\r\n{Log(4)}{errorLine!}");

                    Match match = Regex.Match(errorLine!, "(^.*)\\((\\d*),(\\d*)\\)");

                    if (match.Success)
                    {
                        string file = match.Groups[1].Value.Replace($"D:\\a\\{repo}\\{repo}\\", "");//.Replace('\\', '/');
                        int lineNumber = int.Parse(match.Groups[2].Value);
                        int column = int.Parse(match.Groups[3].Value);
                        bool errorSkipped = false;

                        // Skippable error, only need to find one. LINQ is more expressive then a foreach + if check
                        foreach (var _ in from error in config.ExpectedErrors
                                          where error.Error == errorCode
                                                && file.EndsWith(config.ExpectedErrors[0].File, StringComparison.OrdinalIgnoreCase)
                                                && error.Line == lineNumber
                                          select new { })
                        {
                            Console.WriteLine($"{Log(4)}Skipping this error");
                            errorSkipped = true;
                            break;
                        }

                        if (errorSkipped)
                            errorsSkipped++;
                        else
                            Console.WriteLine($"::error file={file},line={lineNumber},col={column}::{errorLine}");

                    }
                    else
                        Console.WriteLine($"{Log(2)}Unable to parse error line and column");
                }

                // Mark this as successful because every error was skipped
                config.RunConsideredGood = errorsSkipped == everyError.Count;
            }
        }
    }

    
    /// <summary>
    /// Simple function return an indented string. Smaller name so easier to interpolate in strings.
    /// </summary>
    /// <param name="level">How many spaces to add.</param>
    private static string Log(int level) =>
        new string(' ', level);
}
