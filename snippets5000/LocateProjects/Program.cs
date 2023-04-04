using DotNetDocs.Tools.Utility;

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
                throw new ArgumentNullException("The dryrun Test DataFile must be set.");
            else
                projects = new TestingProjectList(dryrunTestId, dryrunTestDateFile, sourcepath).GenerateBuildList();

            Console.WriteLine("Processing results...");

            // ERROR no project
            bool first = false;
            foreach (var project in projects.Where(p => p.Code == DiscoveryResult.RETURN_NOPROJ))
            {
                if (!first) { Console.WriteLine(OUTPUT_ERROR_1_NOPROJ); first = true; }
                Console.WriteLine($"::error file={project.InputFile},line=0,col=0::{OUTPUT_ERROR_1_NOPROJ}");
            }

            // ERROR too many projects
            first = false;
            foreach (var project in projects.Where(p => p.Code == DiscoveryResult.RETURN_TOOMANY))
            {
                if (!first) { Console.WriteLine(OUTPUT_ERROR_2_TOOMANY); first = true; }
                Console.WriteLine($"::error file={project.InputFile},line=0,col=0::{OUTPUT_ERROR_2_TOOMANY}");
            }

            // ERROR solution but no proj
            first = false;
            foreach (var project in projects.Where(p => p.Code == DiscoveryResult.RETURN_SLN))
            {
                if (!first) { Console.WriteLine(OUTPUT_ERROR_3_SLNNOPROJ); first = true; }
                Console.WriteLine($"::error file={project.InputFile},line=0,col=0::{OUTPUT_ERROR_3_SLNNOPROJ}");
            }

            // NO ERROR output each item
            foreach (var project in projects.Where(p => p.Code == DiscoveryResult.RETURN_GOOD))
                Console.WriteLine(project);

            Console.WriteLine("Gathering unique projects to test");

            // Gather the files to be tested:
            foreach (var item in projects.Where(p => p.Code == DiscoveryResult.RETURN_GOOD).Select(p => p.DiscoveredFile).Distinct())
            {
                Console.WriteLine($"TEST: {item}");
            }
        }
        else
        {
            var fullBuild = new FullBuildProjectList(sourcepath);
            foreach (var path in fullBuild.GenerateBuildList())
                Console.WriteLine(path);
        }
        return 0;
    }
}
