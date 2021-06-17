using MarkdownLinksVerifier.LinkValidator;
using Xunit;

namespace MarkdownLinksVerifier.UnitTests.LinkValidatorTests
{
    public class MailtoLinkValidatorTests
    {
        [Fact]
        public void TestEmptyMailto()
        {
            Assert.False(new MailtoLinkValidator().IsValid("mailto:", "UNUSED"));
        }

        [Fact]
        public void TestInvalidEmail()
        {
            Assert.False(new MailtoLinkValidator().IsValid("mailto:person", "UNUSED"));
        }

        [Fact]
        public void TestValidEmail()
        {
            Assert.True(new MailtoLinkValidator().IsValid("mailto:person@company.com", "UNUSED"));
        }
    }
}
