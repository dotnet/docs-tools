using Octokit;

namespace GitHub
{
    public static class PullRequestFileExtensions
    {
        // Didn't find documentation for these values :(
        private const string FILE_ADDED_STATUS = "added";
        private const string FILE_RENAMED_STATUS = "renamed";
        private const string FILE_REMOVED_STATUS = "removed";

        /// <remarks>
        /// An added file has a non-null <see cref="PullRequestFile.PreviousFileName"/> and <see cref="PullRequestFile.FileName"/>
        /// </remarks>
        public static bool IsRenamed(this PullRequestFile file) => file?.Status == FILE_RENAMED_STATUS;

        /// <remarks>
        /// An added file has a null <see cref="PullRequestFile.PreviousFileName"/>
        /// </remarks>
        public static bool IsAdded(this PullRequestFile file) => file?.Status == FILE_ADDED_STATUS;

        /// <remarks>
        /// An added file has a null <see cref="PullRequestFile.PreviousFileName"/>
        /// </remarks>
        public static bool IsRemoved(this PullRequestFile file) => file?.Status == FILE_REMOVED_STATUS;
    }
}
