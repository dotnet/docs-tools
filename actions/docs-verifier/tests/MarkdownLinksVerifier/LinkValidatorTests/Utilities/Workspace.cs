using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MarkdownLinksVerifier.UnitTests.LinkValidatorTests
{
    internal sealed class Workspace : IDisposable
    {
        private const string WorkspaceTests = nameof(WorkspaceTests);

        private bool _initialized;
        private bool _disposed;
        private static readonly string _workspacePath;
        private readonly string _testPath;

#pragma warning disable CA1810 // Initialize reference type static fields inline
        static Workspace()
#pragma warning restore CA1810 // Initialize reference type static fields inline
        {
            _workspacePath = Path.Join(Directory.GetCurrentDirectory(), WorkspaceTests);
            Directory.CreateDirectory(_workspacePath);
            Directory.SetCurrentDirectory(_workspacePath);
        }

        public Workspace([CallerMemberName] string testName = null!)
        {
            if (string.IsNullOrWhiteSpace(testName))
            {
                throw new ArgumentException("Invalid member name.");
            }

            _testPath = Path.Join(_workspacePath, testName);
            if (Directory.Exists(_testPath))
            {
                throw new InvalidOperationException($"Cannot create a workspace with existing directory '{_testPath}'.");
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
                string filePath = Path.Join(_testPath, path);
                string? containingDirectory = Path.GetDirectoryName(filePath);
                if (containingDirectory is null)
                {
                    throw new InvalidOperationException($"Containing directory for path '{filePath}' is null.");
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

            if (Directory.Exists(_testPath))
            {
                Directory.Delete(_testPath, recursive: true);
            }

            _disposed = true;
        }
    }
}
