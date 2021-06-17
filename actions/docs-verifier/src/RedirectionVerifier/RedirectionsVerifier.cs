using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RedirectionVerifier
{
    public static class RedirectionsVerifier
    {
        /// <summary>
        /// Verifies a redirection for the given source path, and write logs (using GitHub-specific syntax) to a text writer.
        /// </summary>
        /// <returns>Returns <see langword="true"/> for a valid redirection; <see langword="false"/> otherwise.</returns>
        public static async Task<bool> WriteResultsAsync(TextWriter writer, string sourcePath)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            List<Redirection> foundRedirections = OpenPublishingRedirectionReader.GetRedirections().Where(redirection => redirection.SourcePath == sourcePath).ToList();
            if (foundRedirections.Count == 0)
            {
                await writer.WriteLineAsync($"::error::No redirection is found for '{sourcePath}'.");
                return false;
            }
            else if (foundRedirections.Count > 1)
            {
                await writer.WriteLineAsync($"::error::Found {foundRedirections.Count} redirections for '{sourcePath}'. Only one redirection is expected per source path.");
                return false;
            }

            Redirection foundRedirection = foundRedirections[0];
            if (foundRedirection.RedirectUrl.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                // A little bit of heuristic.
                await writer.WriteLineAsync($"::error::Redirection found for '{sourcePath}' ends with '.md'. Redirections shouldn't end with '.md'.");
                return false;
            }

            // TODO: Verify file existence if it starts with "/<our_docset>". Will this require setting the docset as an env variable?.
            return true;
        }
    }
}
