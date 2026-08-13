using SmartResumeAnalyzer.Models;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;

namespace SmartResumeAnalyzer.Services
{
    public class ATSScoringService
    {
        private readonly IMemoryCache _cache;

        public ATSScoringService(IMemoryCache cache)
        {
            _cache = cache;
        }
        private static readonly HashSet<string> SkillStopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "experience",
            "skills",
            "professional",
            "work",
            "using",
            "with",
            "and",
            "the",
            "for",
            "based",
            "applicant",
            "tracking",
            "system",
            "resume",
            "cv",
            "job",
            "role",
            "roles",
            "position",
            "positions",
            "project",
            "projects",
            "team",
            "teams",
            "developed",
            "development",
            "years",
            "year",
            "also",
            "skills",
            "language",
            "languages"
        };

        // Technology stacks for recommendations
        private static readonly Dictionary<string, string[]> TechStacks = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Frontend", new[] { "React", "Vue", "Angular", "JavaScript", "TypeScript", "HTML", "CSS", "Next.js", "Svelte" } },
            { "Backend", new[] { "Node.js", "Express", "Python", "Django", "Flask", ".NET", "C#", "Java", "Spring", "Go" } },
            { "DevOps", new[] { "Docker", "Kubernetes", "AWS", "Azure", "GCP", "CI/CD", "Jenkins", "GitLab", "GitHub Actions" } },
            { "Mobile", new[] { "React Native", "Flutter", "Swift", "Kotlin", "iOS", "Android" } },
            { "Data", new[] { "SQL", "MongoDB", "PostgreSQL", "MySQL", "Data Science", "Machine Learning", "Python", "R", "Spark", "Hadoop" } },
            { "Cloud", new[] { "AWS", "Azure", "Google Cloud", "Serverless", "Lambda", "Cloud Functions" } }
        };

        private static readonly Dictionary<string, string[]> CareerPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Full Stack", new[] { "React", "Node.js", "Express", "MongoDB", "PostgreSQL", "C#", ".NET" } },
            { "Frontend Engineer", new[] { "React", "Vue", "Angular", "TypeScript", "JavaScript", "CSS" } },
            { "Backend Engineer", new[] { "Python", "Java", "C#", ".NET", "Node.js", "PostgreSQL", "MySQL", "MongoDB" } },
            { "DevOps Engineer", new[] { "Docker", "Kubernetes", "AWS", "Azure", "CI/CD", "Linux", "Terraform" } },
            { "Cloud Architect", new[] { "AWS", "Azure", "GCP", "Serverless", "Microservices", "Docker" } },
            { "Data Engineer", new[] { "SQL", "Python", "Spark", "Hadoop", "PostgreSQL", "MongoDB" } },
            { "Mobile Developer", new[] { "React Native", "Flutter", "Swift", "Kotlin" } },
            { "Solutions Architect", new[] { "AWS", "Azure", "System Design", "Microservices", ".NET", "Java" } }
        };

        public async Task<ResumeResult> CalculateATSAsync(
            string resumeText,
            string skillsInput)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(resumeText))
                throw new ArgumentException("Resume text cannot be empty", nameof(resumeText));
            if (string.IsNullOrWhiteSpace(skillsInput))
                throw new ArgumentException("Skills input cannot be empty", nameof(skillsInput));

            var key = CreateCacheKey(resumeText, skillsInput, "ats");
            if (_cache.TryGetValue<ResumeResult>(key, out var cached))
                return cached ?? throw new InvalidOperationException("Cached result is null");

            var result = await Task.Run(() =>
            {
                var skills = ParseSkills(skillsInput);

                int matchedSkills = 0;
                var missingSkills = new List<string>();

                foreach (var skill in skills)
                {
                    if (ContainsSkillInResume(resumeText, skill))
                    {
                        matchedSkills++;
                    }
                    else
                    {
                        missingSkills.Add(skill);
                    }
                }

                int score = 0;
                if (skills.Count > 0)
                {
                    score = (int)(((double)matchedSkills / skills.Count) * 100);
                }

                // Extract CV information for more intelligent suggestions
                var detectedSkills = ExtractTechnologiesFromResume(resumeText);
                var experienceLevel = DetermineExperienceLevel(resumeText);

                var suggestions = BuildIntelligentSuggestions(score, skills.Count, detectedSkills, missingSkills, experienceLevel, resumeText);
                var recommendation = BuildRealisticRecommendation(detectedSkills, experienceLevel, score) ?? "Software Developer";

                return new ResumeResult
                {
                    ATSScore = score,
                    MatchedSkills = matchedSkills,
                    MissingSkills = missingSkills ?? new List<string>(),
                    Suggestions = suggestions ?? new List<string>(),
                    CareerRecommendation = recommendation
                };
            });

            _cache.Set(key, result, TimeSpan.FromMinutes(5));
            return result;
        }

        private static string CreateCacheKey(string resumeText, string input, string prefix)
        {
            using var sha = SHA256.Create();
            var combined = (resumeText ?? string.Empty) + "|" + (input ?? string.Empty);
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
            return prefix + ":" + Convert.ToHexString(hash);
        }

        private static List<string> ParseSkills(string skillsInput)
        {
            if (string.IsNullOrWhiteSpace(skillsInput))
                return new List<string>();

            var separators = new[] { ',', ';', '|', '\n', '\r' };
            var parts = skillsInput
                .Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToList();

            if (parts.Count <= 1)
            {
                parts = Regex.Matches(skillsInput, @"[A-Za-z#\+\.]+(?:\s+[A-Za-z#\+\.]+)*")
                    .Select(match => match.Value.Trim())
                    .Where(IsValidSkillPhrase)
                    .ToList();
            }

            return parts
                .Select(CleanSkillPhrase)
                .Where(phrase => !string.IsNullOrWhiteSpace(phrase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool IsValidSkillPhrase(string phrase)
        {
            if (string.IsNullOrWhiteSpace(phrase))
                return false;

            phrase = phrase.Trim();
            if (phrase.Length < 2 || phrase.Length > 60)
                return false;

            var words = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 4)
                return false;

            return words.Any(word =>
                word.Length > 1 &&
                !SkillStopWords.Contains(word) &&
                Regex.IsMatch(word, @"[A-Za-z#\+]+"));
        }

        private static string CleanSkillPhrase(string phrase)
        {
            return Regex.Replace(phrase.Trim(), @"[^A-Za-z0-9#\+\. ]+", string.Empty).Trim();
        }

        private static bool ContainsSkillInResume(string resumeText, string skill)
        {
            if (string.IsNullOrWhiteSpace(skill))
                return false;

            var escaped = Regex.Escape(skill);
            var pattern = $"(?<![A-Za-z0-9_]){escaped}(?![A-Za-z0-9_])";
            return Regex.IsMatch(resumeText ?? string.Empty, pattern, RegexOptions.IgnoreCase);
        }

        // Extract technologies detected in the resume
        private static HashSet<string> ExtractTechnologiesFromResume(string resumeText)
        {
            var detected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var allTechs = new List<string>();

            foreach (var stack in TechStacks.Values)
            {
                allTechs.AddRange(stack);
            }

            foreach (var tech in allTechs)
            {
                if (ContainsSkillInResume(resumeText, tech))
                {
                    detected.Add(tech);
                }
            }

            return detected;
        }

        // Determine experience level based on resume content
        private static string DetermineExperienceLevel(string resumeText)
        {
            var lowerText = resumeText?.ToLower() ?? "";
            
            // Count experience indicators
            var seniorPatterns = new[] { "senior", "lead", "architect", "director", "staff", "10+ years", "15+ years", "20+ years" };
            var midPatterns = new[] { "mid-level", "5-10 years", "5+ years", "7+ years", "8+ years", "9+ years" };
            var juniorPatterns = new[] { "junior", "entry-level", "graduate", "0-2 years", "2-3 years", "3+ years", "intern" };

            var seniorCount = seniorPatterns.Count(p => lowerText.Contains(p));
            var midCount = midPatterns.Count(p => lowerText.Contains(p));
            var juniorCount = juniorPatterns.Count(p => lowerText.Contains(p));

            // Also check for years of experience mentions
            var yearsMatch = Regex.Match(lowerText, @"(\d+)\s*(?:\+)?\s*years");
            if (yearsMatch.Success && int.TryParse(yearsMatch.Groups[1].Value, out var years))
            {
                if (years >= 10) seniorCount += 2;
                else if (years >= 5) midCount += 2;
                else if (years >= 2) juniorCount += 1;
            }

            // Multiple projects/roles suggest more experience
            var projectCount = Regex.Matches(lowerText, @"\b(?:project|developed|built|implemented)\b").Count;
            if (projectCount >= 5) midCount += 1;
            if (projectCount >= 8) seniorCount += 1;

            if (seniorCount > 0 && seniorCount >= midCount && seniorCount >= juniorCount)
                return "Senior";
            else if (midCount > 0 && midCount >= juniorCount)
                return "Mid-Level";
            else
                return "Junior";
        }

        // Build intelligent suggestions based on actual CV content
        private static List<string> BuildIntelligentSuggestions(
            int score, 
            int skillCount, 
            HashSet<string> detectedSkills,
            List<string> missingSkills,
            string experienceLevel,
            string resumeText)
        {
            var suggestions = new List<string>();

            if (skillCount == 0)
            {
                suggestions.Add("Enter the skills you want to target so the analysis can compare them against your resume.");
                suggestions.Add("List core tools, programming languages, and frameworks in a skills section or summary.");
                return suggestions;
            }

            if (missingSkills.Count > 0)
            {
                var topMissing = missingSkills.Take(3).ToList();
                var missingText = topMissing.Count == 1
                    ? topMissing[0]
                    : string.Join(", ", topMissing.Take(topMissing.Count - 1)) + " and " + topMissing.Last();
                suggestions.Add($"Add missing keywords to your resume: {missingText}.");
            }

            if (score < 50)
            {
                var missingByCategory = FindMissingSkillsCategory(missingSkills);
                if (!string.IsNullOrEmpty(missingByCategory))
                {
                    suggestions.Add($"Your resume is missing key {missingByCategory} skills; add them to a dedicated skills section and experience bullets.");
                }
                suggestions.Add("Simplify formatting with plain text bullet points and avoid headings or graphics that ATS may skip.");
            }
            else if (score < 80)
            {
                if (missingSkills.Count > 0)
                {
                    suggestions.Add($"Place {string.Join(", ", missingSkills.Take(2))} in your experience bullets so ATS sees them in context.");
                }
                suggestions.Add("Strengthen your resume with measurable results, such as percentages, team size, or outcomes.");
            }
            else
            {
                suggestions.Add("Strong keyword coverage. Keep those skills visible in your top experience and summary sections.");
            }

            if (detectedSkills.Count > 0)
            {
                var frontendSkills = detectedSkills.Where(s => TechStacks["Frontend"].Contains(s, StringComparer.OrdinalIgnoreCase)).ToList();
                var backendSkills = detectedSkills.Where(s => TechStacks["Backend"].Contains(s, StringComparer.OrdinalIgnoreCase)).ToList();
                var devopsSkills = detectedSkills.Where(s => TechStacks["DevOps"].Contains(s, StringComparer.OrdinalIgnoreCase)).ToList();
                var dataSkills = detectedSkills.Where(s => TechStacks["Data"].Contains(s, StringComparer.OrdinalIgnoreCase)).ToList();

                if (frontendSkills.Count >= 2 && backendSkills.Count == 0)
                {
                    suggestions.Add("You have strong frontend keywords; add backend or cloud keywords to broaden your ATS match.");
                }
                if (backendSkills.Count >= 2 && frontendSkills.Count == 0)
                {
                    suggestions.Add("You have solid backend keywords; include frontend technologies if you want a fuller developer profile.");
                }
                if (devopsSkills.Count >= 2)
                {
                    suggestions.Add("Your DevOps keywords are strong; emphasize cloud certifications and infrastructure outcomes.");
                }
                if (dataSkills.Count >= 2)
                {
                    suggestions.Add("Your data skills are visible; support them with concrete projects and analytic results.");
                }
            }

            var hasMetrics = Regex.IsMatch(resumeText ?? string.Empty, @"(\d+%|\$\d+|\d+x|\d+\+)");
            if (!hasMetrics && score < 90)
            {
                suggestions.Add("Add measurable results to your most important achievements (for example, 'reduced costs by 22%' or 'managed a team of 5').");
            }

            var hasActionVerbs = Regex.IsMatch(resumeText ?? string.Empty, @"\b(spearheaded|orchestrated|architected|engineered|optimized|scaled|delivered|led)\b", RegexOptions.IgnoreCase);
            if (!hasActionVerbs && experienceLevel != "Junior")
            {
                suggestions.Add("Use active verbs in your accomplishments, such as 'Delivered', 'Optimized', 'Led', or 'Architected'.");
            }

            suggestions = suggestions.Distinct().Take(5).ToList();
            return suggestions;
        }

        // Find which category the missing skills belong to
        private static string? FindMissingSkillsCategory(List<string> missingSkills)
        {
            if (missingSkills.Count == 0) return null;

            var categories = new Dictionary<string, int>();
            foreach (var skill in missingSkills.Take(3))
            {
                foreach (var kvp in TechStacks)
                {
                    if (kvp.Value.Any(s => StringComparer.OrdinalIgnoreCase.Equals(s, skill)))
                    {
                        categories[kvp.Key] = categories.ContainsKey(kvp.Key) ? categories[kvp.Key] + 1 : 1;
                    }
                }
            }

            return categories.OrderByDescending(x => x.Value).FirstOrDefault().Key;
        }

        // Build realistic career recommendations based on actual detected skills
        private static string BuildRealisticRecommendation(HashSet<string> detectedSkills, string experienceLevel, int score)
        {
            var recommendations = new List<(string role, int score)>();

            // Score each career path based on detected skills
            foreach (var careerPath in CareerPaths)
            {
                var matchedSkills = careerPath.Value
                    .Count(s => detectedSkills.Any(ds => StringComparer.OrdinalIgnoreCase.Equals(ds, s)));
                
                var pathScore = (matchedSkills * 100) / careerPath.Value.Length;
                recommendations.Add((careerPath.Key, pathScore));
            }

            // Get top recommendation
            var topRecommendation = recommendations.OrderByDescending(x => x.score).FirstOrDefault();

            if (topRecommendation.score >= 50)
            {
                // Add experience level context
                var prefix = experienceLevel switch
                {
                    "Senior" => "Senior ",
                    "Mid-Level" => "",
                    _ => "Junior "
                };

                return $"{prefix}{topRecommendation.role}";
            }

            // Fallback based on detected stacks and experience
            if (detectedSkills.Count == 0)
            {
                return experienceLevel switch
                {
                    "Senior" => "Senior Software Engineer",
                    "Mid-Level" => "Software Engineer",
                    _ => "Junior Developer"
                };
            }

            // If score is very low, suggest reskilling
            if (score < 30)
            {
                return $"Entry-Level {detectedSkills.FirstOrDefault() ?? "IT"} Specialist";
            }

            return "Full Stack Developer";
        }
    }
}
