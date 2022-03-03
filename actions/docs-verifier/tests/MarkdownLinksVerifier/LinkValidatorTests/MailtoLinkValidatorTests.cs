using MarkdownLinksVerifier.LinkValidator;
using Xunit;

namespace MarkdownLinksVerifier.UnitTests.LinkValidatorTests
{
    public class MailtoLinkValidatorTests
    {
        [Fact]
        public void TestEmptyMailto()
        {
            Assert.Equal(ValidationState.LinkNotFound, new MailtoLinkValidator().Validate("mailto:", "UNUSED").State);
        }

        [Fact]
        public void TestInvalidEmail()
        {
            Assert.Equal(ValidationState.LinkNotFound, new MailtoLinkValidator().Validate("mailto:person", "UNUSED").State);
        }

        [Fact]
        public void TestValidEmail()
        {
            Assert.Equal(ValidationState.Valid, new MailtoLinkValidator().Validate("mailto:person@company.com", "UNUSED").State);
        }
    }
}
