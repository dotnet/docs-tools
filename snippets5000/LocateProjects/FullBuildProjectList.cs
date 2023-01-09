namespace LocateProjects
{
    internal class FullBuildProjectList
    {
        private string _rootDir;

        internal FullBuildProjectList(string rootDir) => _rootDir = rootDir;

        internal IEnumerable<string> GenerateBuildList()
        {
            var solutions = findBuildProjects(_rootDir, "*.sln");
            foreach (var solution in solutions)
                yield return solution;

            var folders = (from f in solutions
                           select Path.GetDirectoryName(f)).Distinct().ToArray();
            var csprojs = findBuildProjects(_rootDir, "*.csproj")
                .Where(proj => !proj.ContainedInOneOf(folders));
            foreach (var proj in csprojs)
                yield return proj;
            var fsprojs = findBuildProjects(_rootDir, "*.fsproj")
                .Where(proj => !proj.ContainedInOneOf(folders));
            foreach (var proj in fsprojs)
                yield return proj;
            var vbprojs = findBuildProjects(_rootDir, "*.vbproj")
                .Where(proj => !proj.ContainedInOneOf(folders));
            foreach (var proj in vbprojs)
                yield return proj;
            var vcxprojs = findBuildProjects(_rootDir, "*.vcxproj")
                .Where(proj => !proj.ContainedInOneOf(folders));
            foreach (var proj in vcxprojs)
                yield return proj;
        }

        private static IEnumerable<string> findBuildProjects(string rootDir, string pattern)
        {
            var currentDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(rootDir);

            foreach (var file in Directory.EnumerateFiles(".", pattern, SearchOption.AllDirectories))
                yield return file;

            Directory.SetCurrentDirectory(currentDir);
        }

    }
}
