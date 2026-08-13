using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Caching.Memory;
using SmartResumeAnalyzer.Services;

namespace SmartResumeAnalyzer.Tests
{
    public class ATSScoringServiceTests
    {
        [Fact]
        public async Task CalculateATSAsync_ReturnsExpectedScore()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new ATSScoringService(cache);

            var resume = "C# .NET Developer with 5 years experience. Worked with React and Docker.";
            var skills = "C#, .NET, Docker, SQL";

            var result = await service.CalculateATSAsync(resume, skills);

            Assert.True(result.ATSScore > 0);
            // Ensure at least one skill was matched from the provided skills
            Assert.True(result.MatchedSkills > 0, "Expected at least one matched skill from the resume");
        }
    }
}
