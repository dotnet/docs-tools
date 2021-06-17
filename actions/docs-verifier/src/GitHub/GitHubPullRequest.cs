using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace GitHub
{
    public static class GitHubPullRequest
    {
        // Documented in https://docs.github.com/en/actions/reference/environment-variables#default-environment-variables.
        private const string GITHUB_REPO_ENV_VARIABLE = "GITHUB_REPOSITORY";

        // env:
        //   GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        private const string GITHUB_TOKEN_ENV_VARIABLE = "GITHUB_TOKEN";

        /// <summary>
        /// Given a pull request number, retrieve the files.
        /// </summary>
        /// <param name="pullRequestNumber">The number of the target pull request.</param>
        /// <exception cref="InvalidOperationException">
        /// <para>The exception is thrown if 'GITHUB_REPOSITORY' environment variable isn't found, or if its value is invalid (doesn't contain exactly one slash).</para>
        /// <para>The exception is thrown if 'GITHUB_TOKEN' environment variable isn't found.</para>
        /// </exception>
        public static async Task<IEnumerable<PullRequestFile>> GetPullRequestFilesAsync(int pullRequestNumber)
        {
            string? repository = Environment.GetEnvironmentVariable(GITHUB_REPO_ENV_VARIABLE);
            if (repository is null)
            {
                throw new InvalidOperationException($"Environment variable '{GITHUB_REPO_ENV_VARIABLE}' was not found.");
            }

            string? token = Environment.GetEnvironmentVariable(GITHUB_TOKEN_ENV_VARIABLE);
            if (token is null)
            {
                throw new InvalidOperationException($"Environment variable '{GITHUB_TOKEN_ENV_VARIABLE}' was not found.");
            }

            if (repository.Count(c => c == '/') != 1)
            {
                throw new InvalidOperationException($"Expected exactly one slash in repository name. Repository name found: '{repository}'.");
            }

            string[] userAndRepo = repository.Split('/');
            Debug.Assert(userAndRepo.Length == 2);
            string user = userAndRepo[0];
            string repo = userAndRepo[1];

            var client = new GitHubClient(new ProductHeaderValue("MSDocsBuildVerifier"))
            {
                Credentials = new Credentials(token)
            };

            IReadOnlyList<PullRequestFile> files = await client.PullRequest.Files(user, repo, pullRequestNumber).ConfigureAwait(false);
            return files;
        }
    }
}
