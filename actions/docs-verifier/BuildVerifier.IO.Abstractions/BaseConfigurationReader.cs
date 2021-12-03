using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BuildVerifier.IO.Abstractions
{
    public abstract class BaseConfigurationReader<TConfigurationFile>
        where TConfigurationFile : class
    {
        private static readonly JsonSerializerOptions s_options = new()
        {
            AllowTrailingCommas = true
        };

        private TConfigurationFile? s_cachedConfiguration;
        private bool? s_filesExist;

        /// <summary>
        /// The name of the configuration file(s) on disk.
        /// The names are used relatively with <see cref="File.Exists(string)"/>.
        /// </summary>
        public abstract List<string> ConfigurationFileNames { get; }

        /// <summary>
        /// Reads (or returns the cached) <typeparamref name="TConfigurationFile"/> file.
        /// </summary>
        public async ValueTask<TConfigurationFile?> ReadConfigurationAsync()
        {
            // Only check if the files exist one time.
            if (s_filesExist is null)
            {
                s_filesExist = ConfigurationFileNames.All(fn => File.Exists(fn));
            }

            // If there are cached configuration values, use 'em.
            if (s_cachedConfiguration is not null)
            {
                return s_cachedConfiguration;
            }

            if (s_filesExist.Value)
            {
                // Read the first file.
                string json = await File.ReadAllTextAsync(ConfigurationFileNames[0]);

                // Read any additional files.
                for (var i = 1; i < ConfigurationFileNames.Count; i++)
                {
                    json = String.Concat(json, await File.ReadAllTextAsync(ConfigurationFileNames[i]));
                }

                TConfigurationFile? configuration =
                    JsonSerializer.Deserialize<TConfigurationFile>(json, s_options);

                if (configuration is null)
                {
                    throw new InvalidOperationException($"Failed to read '{ConfigurationFileNames}'.");
                }

                s_cachedConfiguration = configuration;
            }

            return s_cachedConfiguration;
        }
    }
}
