using System;
using System.IO;
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

        private TConfigurationFile? _cachedConfiguration;
        private bool? _fileExists;

        /// <summary>
        /// The name of the configuration file on disk.
        /// The name is used relatively with <see cref="File.Exists(string)"/>.
        /// </summary>
        public abstract string ConfigurationFileName { get; }

        /// <summary>
        /// Reads (or returns the cached) <typeparamref name="TConfigurationFile"/> file.
        /// </summary>
        public async ValueTask<TConfigurationFile?> ReadConfigurationAsync()
        {
            // Only check if the file exists one time.
            if (_fileExists is null)
            {
                _fileExists = File.Exists(ConfigurationFileName);
            }

            // If there are cached configuration values, use 'em.
            if (_cachedConfiguration is not null)
            {
                return _cachedConfiguration;
            }

            if (_fileExists.Value)
            {
                string json = await File.ReadAllTextAsync(ConfigurationFileName);

                TConfigurationFile? configuration =
                    JsonSerializer.Deserialize<TConfigurationFile>(json, s_options);

                if (configuration is null)
                {
                    throw new InvalidOperationException($"Failed to read '{ConfigurationFileName}'.");
                }

                _cachedConfiguration = configuration;
            }
            else
                Console.WriteLine($"Configuration file {ConfigurationFileName} does not exist.");

            return _cachedConfiguration;
        }
    }
}
