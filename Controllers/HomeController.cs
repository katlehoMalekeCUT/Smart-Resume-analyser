using SmartResumeAnalyzer.Models;
using SmartResumeAnalyzer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.IO;
using System.Security.Claims;
using System.Text.Json;

namespace SmartResumeAnalyzer.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        // Services
        private readonly ResumeParserService _parserService;
        private readonly ATSScoringService _atsService;
        private readonly ISupabaseAuthService _supabaseAuthService;

        // Constructor
        public HomeController(
            ResumeParserService parserService,
            ATSScoringService atsService,
            ISupabaseAuthService supabaseAuthService)
        {
            _parserService = parserService;
            _atsService = atsService;
            _supabaseAuthService = supabaseAuthService;
        }

        private async Task<string?> GetValidAccessTokenAsync()
        {
            var refreshToken = await HttpContext.GetTokenAsync("refresh_token");
            if (string.IsNullOrEmpty(refreshToken))
                return null;

            var refreshResult = await _supabaseAuthService.RefreshTokenAsync(refreshToken);
            if (!refreshResult.Success || string.IsNullOrEmpty(refreshResult.AccessToken))
                return null;

            await UpdateAuthenticationTokensAsync(refreshResult, refreshToken);
            return refreshResult.AccessToken;
        }

        private async Task UpdateAuthenticationTokensAsync(SupabaseAuthResult refreshResult, string existingRefreshToken)
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
                return;

            var tokens = authenticateResult.Properties.GetTokens()
                .Where(t => t.Name != "refresh_token" && t.Name != "expires_at")
                .ToList();

            var refreshTokenToStore = !string.IsNullOrEmpty(refreshResult.RefreshToken)
                ? refreshResult.RefreshToken
                : existingRefreshToken;
            if (!string.IsNullOrEmpty(refreshTokenToStore))
                tokens.Add(new AuthenticationToken { Name = "refresh_token", Value = refreshTokenToStore });

            if (refreshResult.ExpiresAt.HasValue)
                tokens.Add(new AuthenticationToken { Name = "expires_at", Value = refreshResult.ExpiresAt.Value.ToString("o") });

            authenticateResult.Properties.StoreTokens(tokens);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, authenticateResult.Principal, authenticateResult.Properties);
        }

        private async Task RefreshUserClaimsAsync(string email, string? fullName, string? contact, string? avatarUrl = null)
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
                return;

            var claims = authenticateResult.Principal.Claims
                .Where(c => c.Type != ClaimTypes.Name && c.Type != "full_name" && c.Type != "contact" && c.Type != "avatar_url" && c.Type != "profile_image_url")
                .ToList();

            claims.Add(new Claim(ClaimTypes.Name, email));
            if (!string.IsNullOrWhiteSpace(fullName))
                claims.Add(new Claim("full_name", fullName));
            if (!string.IsNullOrWhiteSpace(contact))
                claims.Add(new Claim("contact", contact));
            if (!string.IsNullOrWhiteSpace(avatarUrl))
            {
                claims.Add(new Claim("avatar_url", avatarUrl));
                claims.Add(new Claim("profile_image_url", avatarUrl));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authenticateResult.Properties);
        }

        private async Task<(string Email, string FullName, string Contact , string? LinkedInUrl)> GetUserMetadataAsync(string accessToken, string defaultEmail)
        {
            var fullName = defaultEmail;
            var contact = string.Empty;
            string? linkedInUrl = null;

            var (success, userData, error) = await _supabaseAuthService.GetCurrentUserAsync(accessToken);
            if (!success || userData == null)
                return (defaultEmail, fullName, contact,linkedInUrl);

            if (userData.Value.TryGetProperty("email", out var emailProp) && emailProp.ValueKind == JsonValueKind.String)
            {
                defaultEmail = emailProp.GetString() ?? defaultEmail;
            }

            if (userData.Value.TryGetProperty("user_metadata", out var metadata) && metadata.ValueKind == JsonValueKind.Object)
            {
                if (metadata.TryGetProperty("full_name", out var fullNameProp) && fullNameProp.ValueKind == JsonValueKind.String)
                    fullName = fullNameProp.GetString() ?? fullName;
                if (metadata.TryGetProperty("contact", out var contactProp) && contactProp.ValueKind == JsonValueKind.String)
                    contact = contactProp.GetString() ?? contact;
                 if (metadata.TryGetProperty("linkedin_url", out var linkedinProp) && linkedinProp.ValueKind == JsonValueKind.String) { linkedInUrl = linkedinProp.GetString(); }
            }

            return (defaultEmail, fullName, contact , linkedInUrl);
        }

        private static bool IsHttpUrl(string? value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
        }

        private async Task<(string? Url, string? Error)> TryResolveProfileImageUrlAsync(string? storedValue, string accessToken)
        {
            if (string.IsNullOrWhiteSpace(storedValue))
                return (null, null);

            if (IsHttpUrl(storedValue))
                return (storedValue, null);

            var (success, signedUrl, error) = await _supabaseAuthService.GetSignedFileUrlAsync(storedValue, accessToken);
            if (success && !string.IsNullOrEmpty(signedUrl))
                return (signedUrl, null);

            // Fallback to public URL if signing is unavailable or fails.
            try
            {
                var publicUrl = _supabaseAuthService.GetPublicFileUrl(storedValue);
                return (publicUrl, error ?? "Failed to sign URL, using public URL fallback.");
            }
            catch (Exception ex)
            {
                return (null, error ?? ex.Message);
            }
        }

        private async Task<(string Email, string FullName, string Contact, string? ProfileImageUrl, string? LinkedInUrl )> GetUserMetadataWithImageAsync(string accessToken, string defaultEmail)
        {
            var fullName = defaultEmail;
            var contact = string.Empty;
            string? profileImageUrl = null;
            string? linkedInUrl = null;

            var (success, userData, error) = await _supabaseAuthService.GetCurrentUserAsync(accessToken);
            if (!success || userData == null)
                return (defaultEmail, fullName, contact, profileImageUrl,linkedInUrl);

            if (userData.Value.TryGetProperty("email", out var emailProp) && emailProp.ValueKind == JsonValueKind.String)
            {
                defaultEmail = emailProp.GetString() ?? defaultEmail;
            }

            if (userData.Value.TryGetProperty("user_metadata", out var metadata) && metadata.ValueKind == JsonValueKind.Object)
            {
                if (metadata.TryGetProperty("full_name", out var fullNameProp) && fullNameProp.ValueKind == JsonValueKind.String)
                    fullName = fullNameProp.GetString() ?? fullName;
                    
                if (metadata.TryGetProperty("contact", out var contactProp) && contactProp.ValueKind == JsonValueKind.String)
                    contact = contactProp.GetString() ?? contact;
                if (metadata.TryGetProperty("avatar_url", out var avatarProp) && avatarProp.ValueKind == JsonValueKind.String)
                    profileImageUrl = avatarProp.GetString();
                if (metadata.TryGetProperty("profile_image_url", out var profileProp) && profileProp.ValueKind == JsonValueKind.String)
                    profileImageUrl = profileProp.GetString();
              else if (metadata.TryGetProperty("linkedin_url", out var linkedinProp) && linkedinProp.ValueKind == JsonValueKind.String) { linkedInUrl = linkedinProp.GetString();}
            }

            if (!string.IsNullOrEmpty(profileImageUrl) && !IsHttpUrl(profileImageUrl))
            {
                var (resolvedUrl, resolveError) = await TryResolveProfileImageUrlAsync(profileImageUrl, accessToken);
                profileImageUrl = resolvedUrl;
                if (!string.IsNullOrEmpty(resolveError))
                    ViewBag.DebugResolveProfileImageError = resolveError;
            }

            return (defaultEmail, fullName, contact, profileImageUrl, linkedInUrl);
        }

        // =========================
        // HOME PAGE
        // =========================
        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        // =========================
        // ANALYZE PAGE (GET)
        // =========================
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Analyze()
        {
            return View();
        }

        // =========================
        // ANALYZE CV (POST)
        // =========================
        [HttpPost]
        public async Task<IActionResult> Analyze(
            IFormFile resumeFile,
            string skills)
        {
            // Check if file uploaded
            if (resumeFile == null || resumeFile.Length == 0)
            {
                ViewBag.Error = "Please upload your resume.";
                return View();
            }

            var accessToken = await GetValidAccessTokenAsync();
            var userEmail = User.Identity?.Name;
            string? storagePath = null;

            if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(userEmail))
            {
                storagePath = $"user_uploads/{userEmail}/{Guid.NewGuid()}.pdf";
                var uploadResult = await _supabaseAuthService.UploadFileAsync(
                    storagePath,
                    resumeFile.OpenReadStream(),
                    accessToken,
                    resumeFile.ContentType ?? "application/pdf");

                if (!uploadResult.Success)
                {
                    ViewBag.HistoryError = uploadResult.ErrorMessage;
                }
            }

            // Extract text from PDF
            var resumeText =
                await _parserService.ExtractTextAsync(resumeFile);

            // Calculate ATS score
            var result = await _atsService.CalculateATSAsync(resumeText, skills);

            if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(userEmail) && storagePath != null)
            {
                var (userSuccess, userData, userError) = await _supabaseAuthService.GetCurrentUserAsync(accessToken);
                if (userSuccess && userData.HasValue && userData.Value.TryGetProperty("id", out var userIdProp))
                {
                    string? userId = userIdProp.GetString();
                    var suggestionsText = string.Join("; ", result.Suggestions);
                    var saveResult = await _supabaseAuthService.SaveUserRecordAsync(
                        userId ?? string.Empty,
                        userEmail,
                        storagePath,
                        resumeFile.FileName ?? string.Empty,
                        suggestionsText,
                        result.ATSScore,
                        accessToken);
                    if (!saveResult.Success)
                    {
                        ViewBag.HistoryError = saveResult.ErrorMessage;
                    }
                }
                else
                {
                    ViewBag.HistoryError = "Could not retrieve user ID for saving record.";
                }
            }

            var resultModel = new AnalyzeResultViewModel
            {
                ResumeResult = result,
                FileName = resumeFile.FileName ?? string.Empty,
                FilePath = storagePath ?? string.Empty,
                SkillsInput = skills ?? string.Empty,
                SavedSuccessfully = !string.IsNullOrEmpty(storagePath) && !string.IsNullOrEmpty(accessToken),
                Message = string.IsNullOrEmpty(storagePath) ? "Log in to save and download the analyzed resume." : "Your resume has been analyzed and saved."
            };

            return View("Result", resultModel);
        }

        // =========================
        // PRIVACY PAGE
        // =========================
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var loginResult = await _supabaseAuthService.LoginAsync(
                model.Email ?? string.Empty,
                model.Password ?? string.Empty);

            if (!loginResult.Success || string.IsNullOrEmpty(loginResult.AccessToken))
            {
                ModelState.AddModelError(string.Empty, loginResult.Message ?? "Login failed.");
                return View(model);
            }

            var emailClaim = loginResult.Email ?? model.Email ?? string.Empty;
            var fullName = emailClaim;
            var contact = string.Empty;
            string? profileImageUrl = null;

            if (!string.IsNullOrEmpty(loginResult.AccessToken))
            {
                var (userSuccess, userData, userError) = await _supabaseAuthService.GetCurrentUserAsync(loginResult.AccessToken);
                if (userSuccess && userData.HasValue)
                {
                    if (userData.Value.TryGetProperty("user_metadata", out var metadata) && metadata.ValueKind == JsonValueKind.Object)
                    {
                        if (metadata.TryGetProperty("full_name", out var fullNameProp) && fullNameProp.ValueKind == JsonValueKind.String)
                            fullName = fullNameProp.GetString() ?? fullName;
                            string? linkedInUrl = null;
                        if (metadata.TryGetProperty("linkedin_url", out var linkedinProp) && linkedinProp.ValueKind == JsonValueKind.String) { linkedInUrl = linkedinProp.GetString(); }
                        if (metadata.TryGetProperty("contact", out var contactProp) && contactProp.ValueKind == JsonValueKind.String)
                            contact = contactProp.GetString() ?? contact;
                        if (metadata.TryGetProperty("avatar_url", out var avatarProp) && avatarProp.ValueKind == JsonValueKind.String)
                            profileImageUrl = avatarProp.GetString();
                        else if (metadata.TryGetProperty("profile_image_url", out var profileProp) && profileProp.ValueKind == JsonValueKind.String)
                            profileImageUrl = profileProp.GetString();
                    }
                }

                if (!string.IsNullOrEmpty(profileImageUrl) && !IsHttpUrl(profileImageUrl))
                {
                    var (resolvedUrl, resolveError) = await TryResolveProfileImageUrlAsync(profileImageUrl, loginResult.AccessToken);
                    profileImageUrl = resolvedUrl;
                    if (!string.IsNullOrEmpty(resolveError))
                        ViewBag.DebugResolveProfileImageError = resolveError;
                }
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, emailClaim),
                new Claim("full_name", fullName)
            };
            if (!string.IsNullOrWhiteSpace(contact))
                claims.Add(new Claim("contact", contact));
            if (!string.IsNullOrWhiteSpace(profileImageUrl))
                {
                    var safeProfileImageUrl = profileImageUrl!;
                    claims.Add(new Claim("avatar_url", safeProfileImageUrl));
                    claims.Add(new Claim("profile_image_url", safeProfileImageUrl));
                }
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProps = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null
            };
            authProps.StoreTokens(new[]
            {
                new AuthenticationToken { Name = "refresh_token", Value = loginResult.RefreshToken ?? string.Empty },
                new AuthenticationToken { Name = "expires_at", Value = loginResult.ExpiresAt?.ToString("o") ?? string.Empty }
            });

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var registerResult = await _supabaseAuthService.RegisterAsync(
                model.Email ?? string.Empty,
                model.Password ?? string.Empty);

            if (!registerResult.Success)
            {
                ModelState.AddModelError(string.Empty, registerResult.Message ?? "Registration failed.");
                return View(model);
            }

            // Registration successful — send them to enter the verification code instead of logging in directly
            return RedirectToAction("VerifyOtp", new { email = model.Email });
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult VerifyOtp(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> VerifyOtp(string email, string code)
        {
            ViewBag.Email = email;

            var result = await _supabaseAuthService.VerifyOtpAsync(email, code);

            if (!result.Success || string.IsNullOrEmpty(result.AccessToken))
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Verification failed. Please check the code and try again.");
                return View();
            }

            var claims = new List<Claim> { new Claim(ClaimTypes.Name, email) };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProps = new AuthenticationProperties { IsPersistent = false };
            authProps.StoreTokens(new[]
            {
                new AuthenticationToken { Name = "refresh_token", Value = result.RefreshToken ?? string.Empty },
                new AuthenticationToken { Name = "expires_at", Value = result.ExpiresAt?.ToString("o") ?? string.Empty }
            });

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);

            TempData["Message"] = "Your account has been verified!";
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var result = await _supabaseAuthService.ForgotPasswordAsync(email);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Could not send reset code.");
                return View();
            }

            return RedirectToAction("ResetPassword", new { email });
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ResetPassword(string email, string code, string newPassword, string confirmPassword)
        {
            ViewBag.Email = email;

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Passwords do not match.");
                return View();
            }

            var result = await _supabaseAuthService.ResetPasswordWithOtpAsync(email, code, newPassword);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Could not reset password.");
                return View();
            }

            TempData["Message"] = "Password reset successfully! Please log in with your new password.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> ViewRecord(string id)
        {
            var accessToken = await GetValidAccessTokenAsync();
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(userEmail))
                return Challenge();

            var (success, records, message) = await _supabaseAuthService.GetUserRecordsAsync<UserRecord>(userEmail, accessToken);
            if (!success)
                return BadRequest(message);

            var record = records.FirstOrDefault(r => r.Id == id);
            if (record == null)
                return NotFound();

            var detailModel = new RecordDetailViewModel
            {
                Id = record.Id,
                FileName = !string.IsNullOrEmpty(record.FileName) ? record.FileName : Path.GetFileName(record.FilePath),
                FilePath = record.FilePath,
                ATSScore = record.ATSScore ?? 0,
                Suggestions = record.Suggestions,
                Notes = record.Notes,
                CreatedAt = record.CreatedAt
            };

            return View("RecordDetails", detailModel);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var accessToken = await GetValidAccessTokenAsync();
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(userEmail))
                return Challenge();

            var deleteResult = await _supabaseAuthService.DeleteUserRecordAsync(id, accessToken);
            if (!deleteResult.Success)
            {
                TempData["ProfileError"] = deleteResult.ErrorMessage;
            }
            else
            {
                TempData["ProfileSuccess"] = "Analysis record deleted successfully.";
            }

            return RedirectToAction("Profile");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail))
                return Challenge();

            var accessToken = await GetValidAccessTokenAsync();
            var records = new List<UserRecord>();
            var loadMessage = string.Empty;
            var fetchSuccess = false;
            var fullName = userEmail;
            var contact = string.Empty;
            string? profileImageUrl = null;
            string? linkedInUrl = null;

            if (!string.IsNullOrEmpty(accessToken))
            {
                var userInfo = await GetUserMetadataWithImageAsync(accessToken, userEmail);
                userEmail = userInfo.Email;
                fullName = userInfo.FullName;
                contact = userInfo.Contact;
                profileImageUrl = userInfo.ProfileImageUrl;
                linkedInUrl = userInfo.LinkedInUrl;

                if (string.IsNullOrEmpty(profileImageUrl))
                {
                    profileImageUrl = User.FindFirst("avatar_url")?.Value;
                    if (string.IsNullOrEmpty(profileImageUrl))
                        profileImageUrl = User.FindFirst("profile_image_url")?.Value;
                }

                var (success, fetchedRecords, message) = await _supabaseAuthService.GetUserRecordsAsync<UserRecord>(userEmail, accessToken);
                fetchSuccess = success;
                if (success)
                {
                    records = fetchedRecords
                        .OrderByDescending(r => r.CreatedAt ?? DateTime.MinValue)
                        .ToList();
                }
                else
                {
                    loadMessage = message ?? "Failed to load profile history.";
                }
            }
            else
            {
                loadMessage = "Unable to load profile data because the access token is missing.";
            }

            var recentAnalyses = records.Select(r => new ProfileAnalysisEntry
            {
                Id = r.Id,
                FileName = !string.IsNullOrEmpty(r.FileName) ? r.FileName : System.IO.Path.GetFileName(r.FilePath),
                ATSScore = r.ATSScore ?? 0,
                Date = r.CreatedAt
            })
            .Take(6)
            .ToList();

            var allScores = recentAnalyses.Select(r => r.ATSScore).ToList();
            var topSkills = new List<string> { "C#", "ASP.NET", "SQL", "JavaScript" };
            var missingSkills = new List<string> { "React", "Docker", "Azure", "Leadership" };
            var suggestions = records
            .SelectMany(r => (r.Suggestions ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()))
            .Take(5)
            .ToList();

            if (!suggestions.Any())
            {
                suggestions = new List<string>
                {
                    "Add stronger action verbs to your work experience.",
                    "Include measurable project results.",
                    "Highlight cloud or DevOps tools used.",
                    "Match keywords from the job description.",
                    "Use consistent formatting for dates."
                };
            }

            if (!fetchSuccess && string.IsNullOrEmpty(loadMessage))
            {
                loadMessage = "Unable to load profile history at this time.";
            }
            else if (!records.Any() && string.IsNullOrEmpty(loadMessage))
            {
                loadMessage = "No history found yet. Upload and analyze a resume to populate your dashboard.";
            }

            var dateRecords = records.Where(r => r.CreatedAt.HasValue).ToList();
            ViewBag.DebugAvatarClaim = User.FindFirst("avatar_url")?.Value;
            ViewBag.DebugProfileImageClaim = User.FindFirst("profile_image_url")?.Value;
            ViewBag.DebugResolvedProfileImageUrl = profileImageUrl;

            var profileModel = new ProfileViewModel
            {
                FullName = fullName,
                Email = userEmail,
                Contact = contact,
                ProfileImageUrl = profileImageUrl,
                LinkedInUrl = linkedInUrl,
                TotalResumesAnalyzed = records.Count,
                TotalResumesUploaded = records.Count,
                AverageATSScore = allScores.Any() ? (int)Math.Round(allScores.Average()) : 0,
                HighestATSScore = allScores.Any() ? allScores.Max() : 0,
                LastAnalysisDate = dateRecords.Max(r => r.CreatedAt),
                MemberSince = dateRecords.Any() ? dateRecords.Min(r => r.CreatedAt!.Value) : DateTime.Now,
                RecentAnalyses = recentAnalyses,
                TopSkills = topSkills,
                MissingSkills = missingSkills,
                ResumeSuggestions = suggestions,
                LoadMessage = loadMessage
            };

            return View(profileModel);
        }

        [HttpPost]
        public async Task<IActionResult> UploadProfileImage(IFormFile profileImage)
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail))
                return Challenge();

            if (profileImage == null || profileImage.Length == 0)
            {
                TempData["ProfileError"] = "Please select an image file.";
                return RedirectToAction("Profile");
            }

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(profileImage.ContentType?.ToLower() ?? string.Empty))
            {
                TempData["ProfileError"] = "Please upload a valid image file (JPEG, PNG, GIF, or WebP).";
                return RedirectToAction("Profile");
            }

            var accessToken = await GetValidAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                TempData["ProfileError"] = "Session expired. Please log in again.";
                return RedirectToAction("Login");
            }

            try
            {
                // Get file extension
                var fileExtension = Path.GetExtension(profileImage.FileName);
                // Use a simpler path - just store in user_uploads with a profile_pictures prefix
                var storagePath = $"user_uploads/profile_pictures/{userEmail}/{Guid.NewGuid()}{fileExtension}";

                // Upload file to Supabase Storage
                var uploadResult = await _supabaseAuthService.UploadFileAsync(
                    storagePath,
                    profileImage.OpenReadStream(),
                    accessToken,
                    profileImage.ContentType ?? "image/jpeg");

                if (!uploadResult.Success)
                {
                    TempData["ProfileError"] = $"Failed to upload image. {uploadResult.ErrorMessage}";
                    return RedirectToAction("Profile");
                }

                // Update user metadata with the storage path, not the temporary signed URL.
                var updateResult = await _supabaseAuthService.UpdateUserAsync(
                    accessToken,
                    avatarUrl: storagePath);

                if (!updateResult.Success)
                {
                    TempData["ProfileError"] = $"Failed to save profile image. {updateResult.ErrorMessage}";
                    return RedirectToAction("Profile");
                }

                // Resolve a fresh signed URL for immediate display.
                var (signedUrlSuccess, publicImageUrl, signedUrlError) = await _supabaseAuthService.GetSignedFileUrlAsync(storagePath, accessToken);

                if (!signedUrlSuccess || string.IsNullOrEmpty(publicImageUrl))
                {
                    TempData["ProfileError"] = $"Failed to generate image URL. {signedUrlError}";
                    return RedirectToAction("Profile");
                }

                await RefreshUserClaimsAsync(userEmail, null, null, publicImageUrl);

                TempData["ProfileSuccess"] = "Profile image updated successfully!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = $"Error uploading image: {ex.Message}" });
            }
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        // =========================
        // ERROR PAGE
        // =========================
        public IActionResult Error()
        {
            return View();
        }
    }
}