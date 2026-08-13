using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;

namespace SmartResumeAnalyzer.Services
{
    public class VerbMatchingService
    {
        private readonly IMemoryCache _cache;

        // Common action verbs from job descriptions
        private static readonly HashSet<string> ActionVerbs = new(StringComparer.OrdinalIgnoreCase)
        {
            // Leadership & Management
            "lead", "manage", "direct", "oversee", "supervise", "mentor", "coordinate", "delegate",
            "orchestrate", "drive", "spearhead", "champion", "guide",
            
            // Development & Creation
            "develop", "build", "create", "design", "architect", "engineer", "construct", "implement",
            "establish", "innovate", "compose", "establish",
            
            // Analysis & Problem-solving
            "analyze", "evaluate", "assess", "investigate", "examine", "diagnose", "research", "study",
            "optimize", "improve", "enhance", "refactor", "troubleshoot", "debug",
            
            // Communication & Documentation
            "document", "communicate", "present", "report", "articulate", "demonstrate", "explain",
            "translate", "convey", "outline",
            
            // Collaboration & Team work
            "collaborate", "cooperate", "partner", "align", "engage", "integrate", "liaise",
            "support", "assist", "help", "work",
            
            // Planning & Strategy
            "plan", "strategize", "forecast", "project", "propose", "recommend", "outline",
            "scope", "define", "establish",
            
            // Execution & Delivery
            "execute", "implement", "deploy", "launch", "deliver", "release", "roll out",
            "publish", "complete", "finish", "accomplish", "achieve",
            
            // Performance & Results
            "improve", "increase", "decrease", "reduce", "accelerate", "optimize", "enhance",
            "scale", "grow", "expand", "boost", "drive", "maximize", "minimize",
            
            // Quality & Testing
            "test", "validate", "verify", "ensure", "guarantee", "audit", "review", "quality assure",
            
            // Monitoring & Maintenance
            "monitor", "maintain", "sustain", "support", "uphold", "preserve", "track",
            "measure", "observe", "track"
        };

        // Verb stems for matching (helps match variations like develop/developed/developing)
        private static readonly Dictionary<string, string[]> VerbVariations = new(StringComparer.OrdinalIgnoreCase)
        {
            { "develop", new[] { "develop", "developed", "developing", "develops", "development" } },
            { "build", new[] { "build", "built", "building", "builds" } },
            { "design", new[] { "design", "designed", "designing", "designs" } },
            { "implement", new[] { "implement", "implemented", "implementing", "implements", "implementation" } },
            { "manage", new[] { "manage", "managed", "managing", "manages", "management" } },
            { "lead", new[] { "lead", "led", "leading", "leads", "leadership" } },
            { "create", new[] { "create", "created", "creating", "creates", "creation" } },
            { "optimize", new[] { "optimize", "optimized", "optimizing", "optimizes", "optimization" } },
            { "improve", new[] { "improve", "improved", "improving", "improves", "improvement" } },
            { "increase", new[] { "increase", "increased", "increasing", "increases" } },
            { "reduce", new[] { "reduce", "reduced", "reducing", "reduces" } },
            { "accelerate", new[] { "accelerate", "accelerated", "accelerating", "accelerates" } },
            { "scale", new[] { "scale", "scaled", "scaling", "scales" } },
            { "deliver", new[] { "deliver", "delivered", "delivering", "delivers", "delivery" } },
            { "deploy", new[] { "deploy", "deployed", "deploying", "deploys", "deployment" } },
            { "architect", new[] { "architect", "architected", "architectures", "architecture" } },
            { "engineer", new[] { "engineer", "engineered", "engineering", "engineers" } },
            { "analyze", new[] { "analyze", "analyzed", "analyzing", "analyzes", "analysis" } },
            { "test", new[] { "test", "tested", "testing", "tests" } },
            { "mentor", new[] { "mentor", "mentored", "mentoring", "mentors" } },
            { "collaborate", new[] { "collaborate", "collaborated", "collaborating", "collaborates", "collaboration" } }
        };

        public VerbMatchingService(IMemoryCache cache)
        {
            _cache = cache;
        }

        /// <summary>
        /// Extract action verbs from job description
        /// </summary>
        public List<string> ExtractJobVerbs(string jobDescription)
        {
            if (string.IsNullOrWhiteSpace(jobDescription))
                return new List<string>();

            var verbs = new List<string>();
            var lowerText = jobDescription.ToLower();

            // Find sentences that start with action verbs
            var sentences = Regex.Split(jobDescription, @"[.!?;\n]")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim());

            foreach (var sentence in sentences)
            {
                // Extract first verb in sentence (typically after common prepositions)
                var words = sentence.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var word in words)
                {
                    var cleanWord = Regex.Replace(word.ToLower(), @"[^\w]", "");
                    if (ActionVerbs.Contains(cleanWord))
                    {
                        verbs.Add(cleanWord);
                        break; // Get first verb per sentence
                    }
                }
            }

            return verbs.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Calculate how well CV matches job verbs
        /// </summary>
        public async Task<VerbMatchResult> MatchVerbsToResumeAsync(string jobDescription, string resumeText)
        {
            if (string.IsNullOrWhiteSpace(jobDescription))
                throw new ArgumentException("Job description cannot be empty", nameof(jobDescription));
            if (string.IsNullOrWhiteSpace(resumeText))
                throw new ArgumentException("Resume text cannot be empty", nameof(resumeText));

            var key = CreateCacheKey(jobDescription, resumeText, "verbmatch");
            if (_cache.TryGetValue<VerbMatchResult>(key, out var cached))
                return cached ?? throw new InvalidOperationException("Cached result is null");

            var result = await Task.Run(() =>
            {
                var jobVerbs = ExtractJobVerbs(jobDescription);
                var matchedVerbs = new List<string>();
                var missingVerbs = new List<string>();
                var resumeLower = resumeText.ToLower();

                foreach (var verb in jobVerbs)
                {
                    if (VerbAppearsInResume(verb, resumeText))
                    {
                        matchedVerbs.Add(verb);
                    }
                    else
                    {
                        missingVerbs.Add(verb);
                    }
                }

                var score = jobVerbs.Count > 0 
                    ? (int)((double)matchedVerbs.Count / jobVerbs.Count * 100)
                    : 0;

                return new VerbMatchResult
                {
                    Score = score,
                    MatchedVerbs = matchedVerbs,
                    MissingVerbs = missingVerbs,
                    TotalJobVerbs = jobVerbs.Count,
                    TotalMatchedVerbs = matchedVerbs.Count
                };
            });

            _cache.Set(key, result, TimeSpan.FromMinutes(10));
            return result;
        }

        /// <summary>
        /// Check if a verb appears in resume with context
        /// </summary>
        private bool VerbAppearsInResume(string verb, string resumeText)
        {
            var resumeLower = resumeText.ToLower();
            
            // Get all variations of the verb
            var variations = GetVerbVariations(verb);
            
            foreach (var variation in variations)
            {
                // Look for the variation with word boundaries
                var pattern = $@"\b{Regex.Escape(variation)}\b";
                if (Regex.IsMatch(resumeLower, pattern))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get all grammatical variations of a verb
        /// </summary>
        private List<string> GetVerbVariations(string verb)
        {
            var lowerVerb = verb.ToLower();
            
            // Check if we have predefined variations
            if (VerbVariations.TryGetValue(lowerVerb, out var variations))
            {
                return variations.ToList();
            }

            // Generate basic variations
            var basicVariations = new List<string> { lowerVerb };
            
            // Add common endings
            basicVariations.Add(lowerVerb + "ed");      // past tense
            basicVariations.Add(lowerVerb + "ing");     // continuous
            basicVariations.Add(lowerVerb + "s");       // third person
            
            // Handle words ending in 'e'
            if (lowerVerb.EndsWith("e"))
            {
                basicVariations.Add(lowerVerb.Substring(0, lowerVerb.Length - 1) + "ing");
            }
            
            // Handle words ending in 'y'
            if (lowerVerb.EndsWith("y"))
            {
                basicVariations.Add(lowerVerb.Substring(0, lowerVerb.Length - 1) + "ied");
            }

            return basicVariations.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Extract technical skills (non-verbs) from job description
        /// </summary>
        public List<string> ExtractTechnicalSkills(string jobDescription)
        {
            if (string.IsNullOrWhiteSpace(jobDescription))
                return new List<string>();

            var skills = new List<string>();
            
            // Common technology patterns
            var patterns = new[]
            {
                @"\b(Java|Python|C#|C\+\+|JavaScript|TypeScript|Ruby|PHP|Go|Rust|Kotlin|Swift|Objective-C)\b",
                @"\b(React|Vue|Angular|Svelte|Next\.js|Gatsby|Ember)\b",
                @"\b(Node\.js|Express|Django|Flask|Spring|FastAPI|NestJS)\b",
                @"\b(AWS|Azure|Google Cloud|GCP|Heroku)\b",
                @"\b(Docker|Kubernetes|Jenkins|GitLab|GitHub Actions)\b",
                @"\b(MongoDB|PostgreSQL|MySQL|Redis|Elasticsearch|DynamoDB)\b",
                @"\b(REST|GraphQL|gRPC|SOAP)\b",
                @"\b(Machine Learning|AI|Deep Learning|NLP|TensorFlow|PyTorch)\b",
                @"\b(Git|SVN|Mercurial)\b",
                @"\b(Agile|Scrum|Kanban)\b",
                @"\b(Linux|Windows|macOS|Unix)\b",
                @"\b(Microservices|Serverless|Event-Driven)\b"
            };

            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(jobDescription, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    var skill = match.Groups[1].Value;
                    skills.Add(skill);
                }
            }

            return skills.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static string CreateCacheKey(string a, string b, string prefix)
        {
            using var sha = SHA256.Create();
            var combined = (a ?? string.Empty) + "|" + (b ?? string.Empty);
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
            return prefix + ":" + Convert.ToHexString(hash);
        }
    }

    /// <summary>
    /// Result of verb matching between job description and resume
    /// </summary>
    public class VerbMatchResult
    {
        public int Score { get; set; }
        public List<string> MatchedVerbs { get; set; } = new();
        public List<string> MissingVerbs { get; set; } = new();
        public int TotalJobVerbs { get; set; }
        public int TotalMatchedVerbs { get; set; }
    }
}
