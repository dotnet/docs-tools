using MarkdownLinksVerifier.LinkClassifier;
using Xunit;

namespace MarkdownLinksVerifier.UnitTests
{
    public class LinkClassifierTests
    {
        [Fact]
        public void ClassifyEmptyLink_ConsideredAsLocal()
        {
            Assert.Equal(LinkClassification.Local, Classifier.Classify(""));
        }

        [Fact]
        public void ClassifyMailtoLink()
        {
            Assert.Equal(LinkClassification.Mailto, Classifier.Classify("mailto:"));
        }

        [Fact]
        public void ClassifyMailtoLink_CaseInsensitive()
        {
            Assert.Equal(LinkClassification.Mailto, Classifier.Classify("MaILTO:"));
        }

        [Fact]
        public void ClassifyMailtoLink_WithAnActualEmail()
        {
            Assert.Equal(LinkClassification.Mailto, Classifier.Classify("MaILTO:myemail@domain.com"));
        }

        [Fact]
        public void ClassifyHttpLink()
        {
            Assert.Equal(LinkClassification.Online, Classifier.Classify("http://myserver.com"));
        }

        [Fact]
        public void ClassifyHttpsLink()
        {
            Assert.Equal(LinkClassification.Online, Classifier.Classify("https://myserver.com"));
        }

        [Fact]
        public void ClassifyLocalLink_File()
        {
            Assert.Equal(LinkClassification.Local, Classifier.Classify("myfile.txt"));
        }

        [Fact]
        public void ClassifyLocalLink_FileWithDirectory_Backslash()
        {
            Assert.Equal(LinkClassification.Local, Classifier.Classify(@"path\to\myfile.txt"));
        }

        [Fact]
        public void ClassifyLocalLink_FileWithDirectory_ForwardSlash()
        {
            Assert.Equal(LinkClassification.Local, Classifier.Classify(@"path/to/myfile.txt"));
        }

        [Fact]
        public void ClassifyLocalLink_FileWithDirectory_MixedSlashes()
        {
            Assert.Equal(LinkClassification.Local, Classifier.Classify(@"path/to\myfile.txt"));
        }

        [Fact]
        public void ClassifyLocalLink_FromRoot()
        {
            Assert.Equal(LinkClassification.Local, Classifier.Classify(@"~/path/to/file.md"));
        }

        [Fact]
        public void ClassifyLocalLink_FromCurrentDirectory()
        {
            Assert.Equal(LinkClassification.Local, Classifier.Classify(@"./path/to/file.md"));
        }

        [Fact]
        public void ClassifyLocalLink_FromParentDirectory()
        {
            Assert.Equal(LinkClassification.Local, Classifier.Classify(@"../path/to/file.md"));
        }
    }
}
