using DotNetDocs.Tools.Utility;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Text.Json;
using static Snippets5000.SnippetsConfigFile;
using Log = DotNet.DocsTools.Utility.EchoLogging;
using static System.CommandLine.Rendering.Ansi.Cursor;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("PullRequestSimulations")]

namespace Snippets5000;

class Program
{
    const string OUTPUT_ERROR_1_NOPROJ = "ERROR: Project missing. A project (and optionally a solution file) must be in this directory or one of the parent directories to validate and build this code.";
    const string OUTPUT_ERROR_2_TOOMANY = "ERROR: Too many projects found. A single project or solution must exist in this directory or one of the parent directories.";
    const string OUTPUT_ERROR_3_SLNNOPROJ = "ERROR: Solution found, but project was found and isn't in the solution.";
    const string OUTPUT_GOOD = "GOOD: Passed structural tests.";
    const string SNIPPETS_FILE_NAME = "snippets.5000.json";
    
    const int EXITCODE_GOOD = 0;
    const int EXITCODE_BAD = 1;

#if LINUX
    const string FANCY_BATCH_FILENAME = "snippets5000_runner.sh";
#else
    const string FANCY_BATCH_FILENAME = "snippets5000_runner.bat";
#endif

    public const string ENV_EXTENSIONS_PROJECTS_NAME = "ExtensionsProjects";
    public const string ENV_EXTENSIONS_CODE_TRIGGERS_NAME = "ExtensionsCodeTriggers";
    public const string ENV_FILE_TRIGGERS_NAME = "FileTriggers";

    public const string ENV_EXTENSIONS_PROJECTS_DEFAULT = ".sln;.csproj;.fsproj;.vbproj;.vcxproj;.proj";
    public const string ENV_EXTENSIONS_CODE_TRIGGERS_DEFAULT = ".cs;.vb;.fs;.cpp;.h;.xaml;.razor;.cshtml;.vbhtml;.sln;.csproj;.fsproj;.vbproj;.vcxproj;.proj";
    public const string ENV_FILE_TRIGGERS_DEFAULT = "global.json;snippets.5000.json";

    /// <summary>
    /// LocateProjects: Find all projects and solutions requiring a build.
    /// </summary>
    /// <param name="sourcepath">The directory containing the local source tree.</param>
    /// <param name="pullrequest">If available, the number of the pull request being built.</param>
    /// <param name="owner">If available, the owner organization of the repository.</param>
    /// <param name="repo">If available, the name of the repository.</param>
    /// <param name="dryrunTestId">The test id from data.json to simulate a pull request.</param>
    /// <param name="dryrunTestDataFile">The json file defining all the tests that can be referenced by <paramref name="dryrunTestId"/>. Usually data.json.</param>
    /// <returns>0 on success. Otherwise, a non-zero error code.</returns>
    static async Task<int> Main(string sourcepath, int? pullrequest = default, string? owner=default, string? repo=default, string? dryrunTestId=default, string? dryrunTestDataFile=default)
    {
        int exitCode = EXITCODE_GOOD;
        string appStartupFolder = Directory.GetCurrentDirectory();

        if ((pullrequest.HasValue) &&
            !string.IsNullOrEmpty(owner) &&
            !string.IsNullOrEmpty(repo))
        {
            List<DiscoveryResult> projects;

            // Normal github PR
            if (string.IsNullOrEmpty(dryrunTestId))
            {
                var key = CommandLineUtility.GetEnvVariable("GitHubKey", "You must store your GitHub key in the 'GitHubKey' environment variable", null);

                List<DiscoveryResult> localResults = new();
                await foreach (var item in new PullRequestProcessor(owner, repo, pullrequest.Value, sourcepath).GenerateBuildList(key))
                    localResults.Add(item);

                projects = localResults;
            }

            // NOT a normal github PR and instead is a test
            else if (string.IsNullOrEmpty(dryrunTestDataFile))
                throw new ArgumentNullException(nameof(dryrunTestDataFile), "The dryrun Test DataFile must be set");
            else
                projects = new TestingProjectList(dryrunTestId, dryrunTestDataFile, sourcepath).GenerateBuildList().ToList();

            Log.Write(0, $"{projects.Count} items found.");
            Log.Write(0, "\r\nOutput all items found, grouped by status...");

            // Start processing all of the discovered projects
            ProcessDiscoveredProjects(projects, out List<SnippetsConfigFile> transformedProjects, out string[] projectsToCompile);

            // Compile each project
            await CompileProjects(sourcepath, projectsToCompile, transformedProjects);

            // Clear any known errors from the failed projects
            ProcessFailedProjects(repo, transformedProjects.Where(p => p.RunExitCode != 0));

            // Final results. List the projects/files that have failed
            bool first = false;
            var finalFailedProjects = transformedProjects.Where(p => !p.RunConsideredGood).ToArray();
            foreach (var item in finalFailedProjects)
            {
                if (!first)
                {
                    Log.Write(0, "\r\n😭 Compile targets with unresolved issues:");
                    first = true;
                }
                Log.Write(2, item.RunTargetFile);
                exitCode = EXITCODE_BAD;
            }

            // Generate output file
            if (finalFailedProjects.Length != 0)
            {
                Directory.SetCurrentDirectory(appStartupFolder);
                JsonSerializerOptions options = new JsonSerializerOptions() { WriteIndented = true, ReadCommentHandling = JsonCommentHandling.Skip };
                using FileStream file = File.Open("output.json", FileMode.Create);
                JsonSerializer.Serialize(file, finalFailedProjects, options);
            }

            // There were no errors, log it!
            if (exitCode == 0)
                Log.Write(0, "\r\n😀 All builds passing! 😀");

            return exitCode;
        }

        // TODO: building the whole repository
        else
        {
            var fullBuild = new FullBuildProjectList(sourcepath);
            foreach (var path in fullBuild.GenerateBuildList())
                Log.Write(0, path);
        }

        return EXITCODE_GOOD;
    }

    // Takes the discovery results from scanning the files in the PR and checks their status. The projects
    // are all converted into a SnippetsConfigFile object and added to the transformedProjects list, excluding
    // projects that are good and ready to be compiled. Those compile targets are instead added to the
    // projectsToCompile array for processing by the next step.
    private static void ProcessDiscoveredProjects(IEnumerable<DiscoveryResult> projects, out List<SnippetsConfigFile> transformedProjects, out string[] projectsToCompile)
    {
        // Results collection
        transformedProjects = new List<SnippetsConfigFile>();

        // ERROR no project
        bool first = false;
        foreach (var project in projects.Where(p => p.Code == DiscoveryResult.RETURN_NOPROJ))
        {
            if (!first) { Log.Write(0, OUTPUT_ERROR_1_NOPROJ); first = true; }
            Log.Write(0, $"::error file={project.InputFile},line=0,col=0::{OUTPUT_ERROR_1_NOPROJ}");
            transformedProjects.Add(new SnippetsConfigFile() { RunOutput = OUTPUT_ERROR_1_NOPROJ, RunExitCode = project.Code, RunTargetFile = project.InputFile, RunErrorIsStructural = true, RunConsideredGood = false });
        }

        // ERROR too many projects
        first = false;
        foreach (var project in projects.Where(p => p.Code == DiscoveryResult.RETURN_TOOMANY))
        {
            if (!first) { Log.Write(0, OUTPUT_ERROR_2_TOOMANY); first = true; }
            Log.Write(0, $"::error file={project.InputFile},line=0,col=0::{OUTPUT_ERROR_2_TOOMANY}");
            transformedProjects.Add(new SnippetsConfigFile() { RunOutput = OUTPUT_ERROR_2_TOOMANY, RunExitCode = project.Code, RunTargetFile = project.InputFile, RunErrorIsStructural = true, RunConsideredGood = false });
        }

        // TODO: I don't think we want this scenario
        // ERROR solution but no proj
        first = false;
        foreach (var project in projects.Where(p => p.Code == DiscoveryResult.RETURN_SLN_NOPROJ))
        {
            if (!first) { Log.Write(0, OUTPUT_ERROR_3_SLNNOPROJ); first = true; }
            Log.Write(0, $"::error file={project.InputFile},line=0,col=0::{OUTPUT_ERROR_3_SLNNOPROJ}");
            transformedProjects.Add(new SnippetsConfigFile() { RunOutput = OUTPUT_ERROR_3_SLNNOPROJ, RunExitCode = project.Code, RunTargetFile = project.InputFile, RunErrorIsStructural = true, RunConsideredGood = false });
        }

        // NO ERROR output each item
        first = false;
        foreach (var project in projects.Where(p => p.Code == DiscoveryResult.RETURN_GOOD))
        {
            if (!first) { Log.Write(0, OUTPUT_GOOD); first = true; }
            Log.Write(0, project);
        }

        Log.Write(0, "\r\nGathering unique projects to compile:");
        projectsToCompile = projects.Where(p => p.Code == DiscoveryResult.RETURN_GOOD).Select(p => p.DiscoveredFile).Distinct().ToArray();

        // Gather the files to be tested:
        foreach (var item in projectsToCompile)
            Log.Write(2, item);

        Log.Write(0, "\r\nCompile projects...");
    }

    // Compiles all of the projectsToCompile items, adding the results to the transformedProjects list.
    // If a snippets file is found, it's used to generate the SnippetsConfigFile instance.
    private static async Task CompileProjects(string sourcePath, string[] projectsToCompile, List<SnippetsConfigFile> transformedProjects)
    {
        // The variables from the code put into a dictionary. Can be used with the custom
        // command line. Emulates the PowerShell ExpandString system.
        Dictionary<string, string> expansionVariables = new(3);

        // EnvVar is wrong on the github runners!??
        //string visualStudioBatchFile = CommandLineUtility.GetEnvVariable("VS_DEVCMD", "", "C:\\Program Files\\Microsoft Visual Studio\\2022\\Enterprise\\Common7\\Tools\\VsDevCmd.bat");
        string visualStudioBatchFile = "C:\\Program Files\\Microsoft Visual Studio\\2022\\Enterprise\\Common7\\Tools\\VsDevCmd.bat";

        int counter = 1;
        foreach (var item in projectsToCompile)
        {
            expansionVariables.Clear();

            Directory.SetCurrentDirectory(sourcePath);
            Log.CreateGroup($"Compile: {counter}/{projectsToCompile.Length} {item}");

            string projectPath = Path.GetFullPath(item);
            Log.Write(2, $"Resolved path: {projectPath}");

            expansionVariables.Add("repoRoot", sourcePath);
            expansionVariables.Add("projectPath", projectPath);
            expansionVariables.Add("projectDirectory", Path.GetDirectoryName(projectPath)!);

            SnippetsConfigFile config = new();

            // Check if snippets config file exists
            string possibleSnippetsFilePath = Path.Combine(Path.GetDirectoryName(projectPath)!, SNIPPETS_FILE_NAME);
            if (File.Exists(possibleSnippetsFilePath))
            {
                Log.Write(2, "Found snippets config file");

                try
                {
                    config = SnippetsConfigFile.Load(possibleSnippetsFilePath);
                }
                catch (Exception e1)
                {
                    Log.Write(2, $"Unable to load config file: {e1.StackTrace}");
                }
                
            }

            Log.Write(2, $"Mode: {config.Host}");

            Directory.SetCurrentDirectory(Path.GetDirectoryName(projectPath)!);

            // Build the batch file that is run for this project.

            if (config.Host == "dotnet")
            {
#if LINUX
                await File.WriteAllTextAsync(FANCY_BATCH_FILENAME, $"#!/bin/bash\ndotnet build \"{projectPath}\"");
#else
                await File.WriteAllTextAsync(FANCY_BATCH_FILENAME, $"dotnet build \"{projectPath}\"");
#endif
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
                    Log.Write(2, "Mode is custom but command isn't set");
                    config.RunOutput = "Invalid snippets file, missing command for custom action";
                    config.RunConsideredGood = false;
                }
            }
            else
            {
                Log.Write(2, "Mode is invalid... nothing to do");
                config.RunConsideredGood = false;
            }

            config.RunTargetFile = projectPath;

            // Run the batch file to do the compile.
            if (config.RunConsideredGood)
            {
#if LINUX
                Log.Write(2, $"Running linux, setting +x on {FANCY_BATCH_FILENAME}");
                await Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "chmod",
                        ArgumentList = { "+x", FANCY_BATCH_FILENAME }
                    })!.WaitForExitAsync();
#endif

                Log.Write(2, $"Contents of {FANCY_BATCH_FILENAME}:");
                foreach (var line in File.ReadAllLines(FANCY_BATCH_FILENAME))
                    Log.Write(4, line);

                ProcessStartInfo processInfo = new(FANCY_BATCH_FILENAME)
                {
                    WorkingDirectory = Path.GetDirectoryName(projectPath),
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                };

                Process process = new()
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
                Log.Write(2, $"Output:");
                Log.Write(4, config.RunOutput.Replace("\n", $"\n{Log.Ind(4)}"));
            }

            Log.EndGroup();

            transformedProjects.Add(config);

            counter++;
        }

        Log.Write(0, "");
    }

    // After all compiles have finished, this method scans through ones that failed and checks to see
    // if any of the items have a snippets config setting for the failure. If it does, the "failed" item
    // is marked as passing.
    private static void ProcessFailedProjects(string repo, IEnumerable<SnippetsConfigFile> failedProjects)
    {
        bool first = false;

        // Process all of the results and ignore any known errors
        foreach (var config in failedProjects)
        {
            if (config.RunErrorIsStructural) continue;

            if (!first)
            {
                Log.Write(0, "\r\nSome projects failed to compile...");
                first = true;
            }

            Log.Write(0, $"\r\nProcessing failure: {config.RunTargetFile}");

            foreach (var line in config.RunOutput.Split('\n'))
            {
                Match match = Regex.Match(line.Trim(), ": (?:Solution file error|error) ([^:]*)");

                if (match.Success)
                    config.DetectedBuildErrors.Add(new(match.Groups[1].Value, line.Trim(), false));
            }

            if (config.DetectedBuildErrors.Count == 0)
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

                Log.Write(2, "Unable to find error from output");
            }

            // Normal MSBUILD errors
            else
            {
                config.DetectedBuildErrors = config.DetectedBuildErrors.Distinct(new DetectedErrorComparer()).ToList();

                int errorsSkipped = 0;
                foreach (var item in config.DetectedBuildErrors)
                {
                    Log.Write(0, "");
                    Log.Write(0, $"Found error code: {item.ErrorCode} on line\r\n{Log.Ind(4)}{item.ErrorLine!}");
                    Match match = Regex.Match(item.ErrorLine!, "(^.*)\\((\\d*),(\\d*)\\)");

                    if (match.Success)
                    {
                        string file = match.Groups[1].Value.Replace($"D:\\a\\{repo}\\{repo}\\", "");
                        int lineNumber = int.Parse(match.Groups[2].Value);
                        int column = int.Parse(match.Groups[3].Value);
                        bool errorSkipped = false;

                        // Skippable error, only need to find one. LINQ is more expressive then a foreach + if check
                        foreach (var _ in from error in config.ExpectedErrors
                                          where error.Error == item.ErrorCode
                                                && file.EndsWith(config.ExpectedErrors[0].File, StringComparison.OrdinalIgnoreCase)
                                                && error.Line == lineNumber
                                          select new { })
                        {
                            Log.Write(4, "Skipping this error");
                            errorSkipped = true;
                            item.IsSkipped = true;
                            break;
                        }

                        if (errorSkipped)
                            errorsSkipped++;
                        else
                            Log.Write(0, $"::error file={file.Replace('\\', '/')},line={lineNumber},col={column}::{item.ErrorLine}");

                    }
                    else
                        Log.Write(2, "Unable to parse error line and column");
                }

                // Mark this as successful because every error was skipped
                config.RunConsideredGood = errorsSkipped == config.DetectedBuildErrors.Count;
            }
        }
    }
}
