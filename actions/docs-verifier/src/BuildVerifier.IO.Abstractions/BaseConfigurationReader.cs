using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace BuildVerifier.IO.Abstractions;

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
    public string? ConfigurationFileName { get; set; }

    /// <summary>
    /// Reads (or returns the cached) <typeparamref name="TConfigurationFile"/> file.
    /// </summary>
    public async ValueTask<TConfigurationFile?> ReadConfigurationAsync()
    {
        if (_fileExists is null)
        {
            _fileExists = File.Exists(ConfigurationFileName);

            // Try one level deeper.
            if (!(bool)_fileExists)
            {
                string[] subDirs = Directory.GetDirectories(".", "*", SearchOption.TopDirectoryOnly);
                foreach (string dir in subDirs)
                {
                    if (File.Exists($"{dir}/{ConfigurationFileName}"))
                    {
                        ConfigurationFileName = $"{dir}/{ConfigurationFileName}";
                        _fileExists = true;
                        break;
                    }
                }
            }
        }

        // If there are cached configuration values, use 'em.
        if (_cachedConfiguration is not null)
        {
            return _cachedConfiguration;
        }

        if (_fileExists.Value)
        {
            string json = await File.ReadAllTextAsync(ConfigurationFileName!);

            TConfigurationFile? configuration =
                JsonSerializer.Deserialize<TConfigurationFile>(json, s_options) ??
                    throw new InvalidOperationException($"Failed to read '{ConfigurationFileName}'.");
            _cachedConfiguration = configuration;
        }
        else
            Console.WriteLine($"Configuration file {ConfigurationFileName} does not exist.");

        return _cachedConfiguration;
    }
}
