using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Caching.Memory;
using SmartResumeAnalyzer.Services;
using System.Collections.Generic;

namespace SmartResumeAnalyzer.Tests
{
    public class KeywordAnalyzerServiceTests
    {
        [Fact]
        public async Task CalculateScoreAsync_MatchingKeywords_ReturnsPositive()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new KeywordAnalyzerService(cache);

            var resume = "Experienced developer with C# .NET and SQL skills.";
            var job = "We are hiring a C# developer with SQL experience.";

            var score = await service.CalculateScoreAsync(resume, job);
            var missing = await service.MissingKeywordsAsync(resume, job);

            Assert.True(score > 0);
            Assert.IsType<List<string>>(missing);
        }
    }
}
