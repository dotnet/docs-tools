using Octokit;

namespace GitHub
{
    public static class PullRequestFileExtensions
    {
        // Didn't find documentation for these values :(
        private const string FILE_ADDED_STATUS = "added";
        private const string FILE_RENAMED_STATUS = "renamed";
        private const string FILE_REMOVED_STATUS = "removed";

        public static bool IsRenamed(this PullRequestFile file) => file?.Status == FILE_RENAMED_STATUS;

        public static bool IsAdded(this PullRequestFile file) => file?.Status == FILE_ADDED_STATUS;

        public static bool IsRemoved(this PullRequestFile file) => file?.Status == FILE_REMOVED_STATUS;
    }
}
