using System;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;

namespace RedirectionVerifier
{
    internal static class OpenPublishingRedirectionReader
    {
        private static readonly JsonSerializerOptions s_options = new() { AllowTrailingCommas = true };
        private const string OpenPublishingRedirectionFileName = ".openpublishing.redirection.json";

        /// <summary>
        /// Retrieves the redirections from <c>.openpublishing.redirection.json</c>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Failed to read <c>.openpublishing.redirection.json</c>.</exception>
        public static ImmutableArray<Redirection> GetRedirections()
        {
            if (!File.Exists(OpenPublishingRedirectionFileName))
            {
                throw new InvalidOperationException($"File '{OpenPublishingRedirectionFileName}' was not found.");
            }

            string json = File.ReadAllText(OpenPublishingRedirectionFileName);
            OpenPublishingRedirections? redirections = JsonSerializer.Deserialize<OpenPublishingRedirections>(json, s_options);
            if (redirections is null)
            {
                throw new InvalidOperationException($"Failed to read '{OpenPublishingRedirectionFileName}'.");
            }

            return redirections.Redirections;
        }
    }
}
