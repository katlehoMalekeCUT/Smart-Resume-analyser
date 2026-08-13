using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartResumeAnalyzer.Services
{
    public class SupabaseAuthService : ISupabaseAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly string _supabaseUrl;
        private readonly string _anonKey;
        private readonly string _bucketName;
        private readonly string _userRecordsTable;

        public SupabaseAuthService(IConfiguration configuration, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _supabaseUrl = configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase URL not configured");
            _anonKey = configuration["Supabase:AnonKey"] ?? throw new InvalidOperationException("Supabase AnonKey not configured");
            _bucketName = configuration["Supabase:BucketName"] ?? throw new InvalidOperationException("Supabase bucket name not configured");
            _userRecordsTable = configuration["Supabase:UserRecordsTable"] ?? "userRecords";
        }

        // (Moved records to ISupabaseAuthService shared types)

        public async Task<SupabaseAuthResult> RegisterAsync(string email, string password)
        {
            try
            {
                var url = $"{_supabaseUrl}/auth/v1/signup";
                var payload = new { email, password };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };
                request.Headers.Add("apikey", _anonKey);

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var tokenData = JsonSerializer.Deserialize<JsonElement>(responseBody);
                    var accessToken = tokenData.TryGetProperty("access_token", out var accessTokenElement)
                        ? accessTokenElement.GetString()
                        : null;
                    var refreshToken = tokenData.TryGetProperty("refresh_token", out var refreshTokenElement)
                        ? refreshTokenElement.GetString()
                        : null;
                    var expiresAt = GetExpiryTime(tokenData);

                    var message = accessToken == null
                        ? "Registration successful. Please check your email to confirm your account."
                        : "Registration successful.";

                    return new SupabaseAuthResult(true, message, email, accessToken, refreshToken, expiresAt);
                }

                var errorMsg = ExtractErrorMessage(responseBody, "Registration failed.");
                return new SupabaseAuthResult(false, errorMsg, null, null, null, null);
            }
            catch (Exception ex)
            {
                return new SupabaseAuthResult(false, $"Error: {ex.Message}", null, null, null, null);
            }
        }

        public async Task<SupabaseAuthResult> LoginAsync(string email, string password)
        {
            try
            {
                var url = $"{_supabaseUrl}/auth/v1/token?grant_type=password";
                var payload = new { email, password };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };
                request.Headers.Add("apikey", _anonKey);

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var tokenData = JsonSerializer.Deserialize<JsonElement>(responseBody);
                    var accessToken = tokenData.TryGetProperty("access_token", out var accessTokenElement)
                        ? accessTokenElement.GetString()
                        : null;
                    var refreshToken = tokenData.TryGetProperty("refresh_token", out var refreshTokenElement)
                        ? refreshTokenElement.GetString()
                        : null;
                    var expiresAt = GetExpiryTime(tokenData);

                    return new SupabaseAuthResult(true, "Login successful", email, accessToken, refreshToken, expiresAt);
                }

                var errorMsg = ExtractErrorMessage(responseBody, "Email or password is incorrect.");
                return new SupabaseAuthResult(false, errorMsg, null, null, null, null);
            }
            catch (Exception ex)
            {
                return new SupabaseAuthResult(false, $"Error: {ex.Message}", null, null, null, null);
            }
        }

        public async Task<SupabaseAuthResult> VerifyOtpAsync(string email, string token, string type = "signup")
        {
            try
            {
                var url = $"{_supabaseUrl}/auth/v1/verify";
                var payload = new { type, email, token };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };
                request.Headers.Add("apikey", _anonKey);

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var tokenData = JsonSerializer.Deserialize<JsonElement>(responseBody);
                    var accessToken = tokenData.TryGetProperty("access_token", out var accessTokenElement)
                        ? accessTokenElement.GetString()
                        : null;
                    var refreshToken = tokenData.TryGetProperty("refresh_token", out var refreshTokenElement)
                        ? refreshTokenElement.GetString()
                        : null;
                    var expiresAt = GetExpiryTime(tokenData);

                    return new SupabaseAuthResult(true, "Verification successful.", email, accessToken, refreshToken, expiresAt);
                }

                var errorMsg = ExtractErrorMessage(responseBody, "Invalid or expired code.");
                return new SupabaseAuthResult(false, errorMsg, null, null, null, null);
            }
            catch (Exception ex)
            {
                return new SupabaseAuthResult(false, $"Error: {ex.Message}", null, null, null, null);
            }
        }

        public async Task<OperationResult> ForgotPasswordAsync(string email)
        {
            try
            {
                var url = $"{_supabaseUrl}/auth/v1/recover";
                var payload = new { email };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };
                request.Headers.Add("apikey", _anonKey);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return new OperationResult(true, null);

                var body = await response.Content.ReadAsStringAsync();
                return new OperationResult(false, ExtractErrorMessage(body, "Could not send reset code."));
            }
            catch (Exception ex)
            {
                return new OperationResult(false, $"Error: {ex.Message}");
            }
        }

        public async Task<OperationResult> ResetPasswordWithOtpAsync(string email, string token, string newPassword)
        {
            try
            {
                // Step 1: verify the recovery code to get a temporary access token
                var verifyResult = await VerifyOtpAsync(email, token, type: "recovery");
                if (!verifyResult.Success || string.IsNullOrEmpty(verifyResult.AccessToken))
                    return new OperationResult(false, verifyResult.Message ?? "Invalid or expired code.");

                // Step 2: use that access token to set the new password
                var url = $"{_supabaseUrl}/auth/v1/user";
                var payload = new { password = newPassword };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Put, url)
                {
                    Content = content
                };
                request.Headers.Add("apikey", _anonKey);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", verifyResult.AccessToken);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return new OperationResult(true, null);

                var body = await response.Content.ReadAsStringAsync();
                return new OperationResult(false, ExtractErrorMessage(body, "Could not reset password."));
            }
            catch (Exception ex)
            {
                return new OperationResult(false, $"Error: {ex.Message}");
            }
        }

        public async Task<SupabaseAuthResult> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var url = $"{_supabaseUrl}/auth/v1/token?grant_type=refresh_token";
                var payload = new { refresh_token = refreshToken };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };
                request.Headers.Add("apikey", _anonKey);

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var tokenData = JsonSerializer.Deserialize<JsonElement>(responseBody);
                    var accessToken = tokenData.TryGetProperty("access_token", out var accessTokenElement)
                        ? accessTokenElement.GetString()
                        : null;
                    var newRefreshToken = tokenData.TryGetProperty("refresh_token", out var refreshTokenElement)
                        ? refreshTokenElement.GetString()
                        : null;
                    var expiresAt = GetExpiryTime(tokenData);

                    return new SupabaseAuthResult(true, "Token refreshed.", null, accessToken, newRefreshToken, expiresAt);
                }

                var errorMsg = ExtractErrorMessage(responseBody, "Token refresh failed.");
                return new SupabaseAuthResult(false, errorMsg, null, null, null, null);
            }
            catch (Exception ex)
            {
                return new SupabaseAuthResult(false, $"Error: {ex.Message}", null, null, null, null);
            }
        }

        public async Task<(bool Success, JsonElement? UserData, string? ErrorMessage)> GetCurrentUserAsync(string accessToken)
        {
            try
            {
                var url = $"{_supabaseUrl}/auth/v1/user";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("apikey", _anonKey);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    return (false, null, ExtractErrorMessage(responseBody, "Could not retrieve user profile."));

                var userData = JsonSerializer.Deserialize<JsonElement>(responseBody);
                return (true, userData, null);
            }
            catch (Exception ex)
            {
                return (false, null, $"Error: {ex.Message}");
            }
        }

        public async Task<OperationResult> UpdateUserAsync(string accessToken, string? email = null, string? fullName = null, string? contact = null, string? avatarUrl = null)
        {
            try
            {
                var url = $"{_supabaseUrl}/auth/v1/user";
                var payload = new Dictionary<string, object>();

                if (!string.IsNullOrWhiteSpace(email))
                    payload["email"] = email;

                var metadata = new Dictionary<string, object>();
                if (fullName != null)
                    metadata["full_name"] = fullName;
                if (contact != null)
                    metadata["contact"] = contact;
                if (avatarUrl != null)
                    metadata["avatar_url"] = avatarUrl;

                if (metadata.Any())
                    payload["user_metadata"] = metadata;

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var request = new HttpRequestMessage(HttpMethod.Patch, url)
                {
                    Content = content
                };
                request.Headers.Add("apikey", _anonKey);
                request.Headers.Add("Accept", "application/json");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                    return new OperationResult(true, null);

                return new OperationResult(false, ExtractErrorMessage(body, "Could not update profile."));
            }
            catch (Exception ex)
            {
                return new OperationResult(false, $"Error: {ex.Message}");
            }
        }

        public string GetPublicFileUrl(string filePath)
        {
            // Encode each path segment but preserve slashes between segments
            var encodedPath = string.Join("/", filePath.Split('/').Select(Uri.EscapeDataString));
            return $"{_supabaseUrl}/storage/v1/object/public/{_bucketName}/{encodedPath}";
        }

        public async Task<(bool Success, string? SignedUrl, string? ErrorMessage)> GetSignedFileUrlAsync(string filePath, string accessToken, int expirationSeconds = 2592000)
        {
            try
            {
                var encodedPath = string.Join("/", filePath.Split('/').Select(Uri.EscapeDataString));
                var url = $"{_supabaseUrl}/storage/v1/object/sign/{_bucketName}/{encodedPath}";

                var payload = new { expiresIn = expirationSeconds };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };
                request.Headers.Add("apikey", _anonKey);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return (false, null, ExtractErrorMessage(responseBody, "Failed to generate signed URL."));
                }

                try
                {
                    var signedUrlData = JsonSerializer.Deserialize<JsonElement>(responseBody);
                    if (signedUrlData.TryGetProperty("signedURL", out var signedUrlProp) && signedUrlProp.ValueKind == JsonValueKind.String)
                    {
                        var signedUrl = signedUrlProp.GetString();
                        // If Supabase returns a path fragment, ensure it's a full URL
                        if (!string.IsNullOrEmpty(signedUrl) && (signedUrl.StartsWith("http://") || signedUrl.StartsWith("https://")))
                            return (true, signedUrl, null);

                        // Otherwise, build full URL
                        return (true, $"{_supabaseUrl}{signedUrl}", null);
                    }
                }
                catch
                {
                    // fall through
                }

                return (false, null, "Could not extract signed URL from response.");
            }
            catch (Exception ex)
            {
                return (false, null, $"Error: {ex.Message}");
            }
        }

        private static DateTime? GetExpiryTime(JsonElement tokenData)
        {
            if (tokenData.TryGetProperty("expires_in", out var expiresInElement) && expiresInElement.ValueKind == JsonValueKind.Number)
            {
                if (expiresInElement.TryGetInt32(out var expiresInSeconds))
                {
                    return DateTime.UtcNow.AddSeconds(expiresInSeconds);
                }
            }

            return null;
        }

        public async Task<OperationResult> UploadFileAsync(string filePath, Stream data, string accessToken, string contentType)
        {
            try
            {
                // Supabase Storage expects a POST to the object path for file upload
                // Build the path by encoding segments individually so slashes are preserved
                var uploadPath = string.Join("/", filePath.Split('/').Select(Uri.EscapeDataString));
                var uploadUrl = $"{_supabaseUrl}/storage/v1/object/{_bucketName}/{uploadPath}";
                using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl)
                {
                    Content = new StreamContent(data)
                };

                // Use a generic content type if the upload service rejects the specific mime type
                var finalContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(finalContentType);
                request.Headers.Add("apikey", _anonKey);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("x-upsert", "true");

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return new OperationResult(true, null);

                var body = await response.Content.ReadAsStringAsync();
                return new OperationResult(false, ExtractErrorMessage(body, "File upload failed."));
            }
            catch (Exception ex)
            {
                return new OperationResult(false, $"Error: {ex.Message}");
            }
        }

        public async Task<OperationResult> SaveUserRecordWithFileNameAsync(string userId, string email, string filePath, string fileName, string suggestions, int? atsScore, string accessToken)
        {
            try
            {
                var insertUrl = $"{_supabaseUrl}/rest/v1/{_userRecordsTable}";
                var payload = new Dictionary<string, object>
                {
                    ["user_id"] = userId,
                    ["user_email"] = email,
                    ["file_path"] = filePath,
                    ["file_name"] = fileName,
                    ["suggestions"] = suggestions,
                    ["ats_score"] = atsScore ?? 0,
                    ["created_at"] = DateTime.UtcNow.ToString("o")
                };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, insertUrl)
                {
                    Content = content
                };
                request.Headers.Add("apikey", _anonKey);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("Prefer", "return=minimal");

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return new OperationResult(true, null);

                var body = await response.Content.ReadAsStringAsync();
                return new OperationResult(false, ExtractErrorMessage(body, "Could not save record."));
            }
            catch (Exception ex)
            {
                return new OperationResult(false, $"Error: {ex.Message}");
            }
        }

        // Compatibility wrapper expected by some controller code paths
        public Task<OperationResult> SaveUserRecordAsync(string userId, string email, string filePath, string fileName, string suggestions, int? atsScore, string accessToken)
        {
            return SaveUserRecordWithFileNameAsync(userId, email, filePath, fileName, suggestions, atsScore, accessToken);
        }

        public async Task<(bool Success, List<T> Records, string? ErrorMessage)> GetUserRecordsAsync<T>(string email, string accessToken)
        {
            try
            {
                var selectUrl = $"{_supabaseUrl}/rest/v1/{_userRecordsTable}?user_email=eq.{Uri.EscapeDataString(email)}&select=*";
                using var request = new HttpRequestMessage(HttpMethod.Get, selectUrl);
                request.Headers.Add("apikey", _anonKey);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    return (false, new List<T>(), ExtractErrorMessage(body, "Could not load history."));

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };
                var records = JsonSerializer.Deserialize<List<T>>(body, options);
                return (true, records ?? new List<T>(), null);
            }
            catch (Exception ex)
            {
                return (false, new List<T>(), $"Error: {ex.Message}");
            }
        }

        public async Task<(bool Success, byte[] Content, string ContentType, string? ErrorMessage)> DownloadFileAsync(string filePath, string accessToken)
        {
            try
            {
                var downloadPath = string.Join("/", filePath.Split('/').Select(Uri.EscapeDataString));
                var downloadUrl = $"{_supabaseUrl}/storage/v1/object/{_bucketName}/{downloadPath}";
                using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
                request.Headers.Add("apikey", _anonKey);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsByteArrayAsync();
                if (!response.IsSuccessStatusCode)
                    return (false, Array.Empty<byte>(), string.Empty, ExtractErrorMessage(await response.Content.ReadAsStringAsync(), "Could not download file."));

                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                return (true, content, contentType, null);
            }
            catch (Exception ex)
            {
                return (false, Array.Empty<byte>(), string.Empty, $"Error: {ex.Message}");
            }
        }

        public async Task<OperationResult> DeleteUserRecordAsync(string id, string accessToken)
        {
            try
            {
                var deleteUrl = $"{_supabaseUrl}/rest/v1/{_userRecordsTable}?id=eq.{Uri.EscapeDataString(id)}";
                using var request = new HttpRequestMessage(HttpMethod.Delete, deleteUrl);
                request.Headers.Add("apikey", _anonKey);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("Prefer", "return=minimal");

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return new OperationResult(true, null);

                var body = await response.Content.ReadAsStringAsync();
                return new OperationResult(false, ExtractErrorMessage(body, "Could not delete record."));
            }
            catch (HttpRequestException ex)
            {
                return new OperationResult(false, $"Error: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                return new OperationResult(false, $"Error: {ex.Message}");
            }
        }

        private static string ExtractErrorMessage(string responseBody, string defaultMessage)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
                return defaultMessage;

            try
            {
                var errorData = JsonSerializer.Deserialize<JsonElement>(responseBody);

                if (errorData.TryGetProperty("error_description", out var description) && description.ValueKind == JsonValueKind.String)
                    return description.GetString() ?? defaultMessage;

                if (errorData.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                    return message.GetString() ?? defaultMessage;

                if (errorData.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
                    return error.GetString() ?? defaultMessage;

                if (errorData.TryGetProperty("msg", out var msg) && msg.ValueKind == JsonValueKind.String)
                    return msg.GetString() ?? defaultMessage;

                return errorData.ToString();
            }
            catch
            {
                return responseBody;
            }
        }
    }
}