using System;
using System.IO;
using System.Threading.Tasks;

namespace MarkdownLinksVerifier.UnitTests.LinkValidatorTests
{
    internal sealed class Workspace : IDisposable
    {
        private const string WorkspaceTests = nameof(WorkspaceTests);

        private bool _initialized;
        private bool _disposed;
        private readonly string _workspacePath;

        public Workspace()
        {
            _workspacePath = Path.Join(Directory.GetCurrentDirectory(), WorkspaceTests);
            if (Directory.Exists(_workspacePath))
            {
                throw new InvalidOperationException($"Cannot create a workspace with existing directory '{_workspacePath}'.");
            }
        }

        public FilesCollection Files { get; } = new();

        /// <summary>
        /// Initializs the workspace by creating all the requested files with their contents.
        /// </summary>
        /// <returns>The path to the workspace folder that contains the requested files.</returns>
        public async Task<string> InitializeAsync()
        {
            if (_initialized)
            {
                throw new InvalidOperationException("The workspace is already initialized");
            }

            foreach ((string path, string contents) in Files)
            {
                string filePath = Path.Join(_workspacePath, path);
                string? containingDirectory = Path.GetDirectoryName(filePath);
                if (containingDirectory is null)
                {
                    throw new InvalidOperationException($"Containing director for path '{filePath}' is null.");
                }

                if (!Directory.Exists(containingDirectory))
                {
                    _ = Directory.CreateDirectory(containingDirectory);
                }

                await File.WriteAllTextAsync(filePath, contents);
            }

            _initialized = true;
            return _workspacePath;

        }

        public void Dispose()
        {
            if (_disposed)
            {
                throw new InvalidOperationException("The workspace is already disposed.");
            }

            if (!_initialized)
            {
                throw new InvalidOperationException("The workspace isn't initialized.");
            }

            Directory.Delete(_workspacePath, recursive: true);
            _disposed = true;
        }
    }
}
