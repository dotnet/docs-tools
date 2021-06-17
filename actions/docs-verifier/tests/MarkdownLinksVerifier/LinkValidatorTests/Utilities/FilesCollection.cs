using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace MarkdownLinksVerifier.UnitTests.LinkValidatorTests
{
    internal sealed class FilesCollection : IEnumerable<KeyValuePair<string, string>>
    {
        private readonly Dictionary<string, string> _files = new();

        public void Add(string path, string contents)
            => _files.Add(path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar), contents);

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _files.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _files.GetEnumerator();
    }
}
