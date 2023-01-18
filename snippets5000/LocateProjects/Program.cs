using DotNetDocs.Tools.Utility;

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
    /// <summary>
    /// LocateProjects: Find all projects and solutions requiring a build.
    /// </summary>
    /// <param name="framework">true if this should build oldstyle framework projects</param>
    /// <param name="pullrequest">If available, the number of the pull request being built</param>
    /// <param name="owner">If available, the owner organization of the repository</param>
    /// <param name="repo">If available, the name of the repository</param>
    /// <param name="argument">The rootdir containing the local source tree</param>
    /// <returns>0 on success. Otherwise, a non-zero error code.</returns>
    /// <remarks>
    /// The output from standard out is the list of all projects and 
    /// solutions that should be built. If nothing but the rootdir is specified,
    /// it will output all solutions, and all projects that are not part of a solution.
    /// </remarks>
    static async Task<int> Main(string argument, bool framework = false, int? pullrequest = default, string? owner=default, string? repo=default)
    {
        if ((pullrequest.HasValue) &&
            !string.IsNullOrEmpty(owner) &&
            !string.IsNullOrEmpty(repo))
        {
            var key = CommandLineUtility.GetEnvVariable("GitHubKey",
            "You must store your GitHub key in the 'GitHubKey' environment variable",
            "");

            var prBuild = new PullRequestProjectList(owner, repo, pullrequest.Value, argument);
            await foreach (var path in prBuild.GenerateBuildList(key))
                Console.WriteLine(path);
        }
        else
        {
            var fullBuild = new FullBuildProjectList(argument);
            foreach (var path in fullBuild.GenerateBuildList())
                Console.WriteLine(path);
        }
        return 0;
    }
}
