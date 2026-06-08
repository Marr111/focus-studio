using FocusDesk.Services;
using Xunit;

namespace FocusDesk.Tests
{
    public class HostsBlockerTests
    {
        [Theory]
        [InlineData("https://www.google.com", "google.com")]
        [InlineData("http://youtube.com/watch?v=123", "youtube.com")]
        [InlineData("www.facebook.com/", "facebook.com")]
        [InlineData("twitter.com", "twitter.com")]
        [InlineData("https://www.linkedin.com/feed/", "linkedin.com")]
        public void NormalizeDomain_ShouldExtractCorrectDomain(string input, string expected)
        {
            var result = HostsBlocker.NormalizeDomain(input);
            Assert.Equal(expected, result);
        }
    }
}
