using System.Text.Json;

namespace SmartResumeAnalyzer.Services
{
    // Lightweight stub used for local development when Supabase config is missing
    public class SupabaseAuthServiceStub : ISupabaseAuthService
    {
        public Task<SupabaseAuthResult> RegisterAsync(string email, string password)
        {
            return Task.FromResult(new SupabaseAuthResult(true, "Registered (stub)", email, "stub-access-token", "stub-refresh", DateTime.UtcNow.AddHours(1)));
        }

        public Task<SupabaseAuthResult> LoginAsync(string email, string password)
        {
            return Task.FromResult(new SupabaseAuthResult(true, "Login successful (stub)", email, "stub-access-token", "stub-refresh", DateTime.UtcNow.AddHours(1)));
        }

        public Task<SupabaseAuthResult> VerifyOtpAsync(string email, string token, string type = "signup")
        {
            return Task.FromResult(new SupabaseAuthResult(true, "Verified (stub)", email, "stub-access-token", "stub-refresh", DateTime.UtcNow.AddHours(1)));
        }

        public Task<OperationResult> ForgotPasswordAsync(string email)
        {
            return Task.FromResult(new OperationResult(true, null));
        }

        public Task<OperationResult> ResetPasswordWithOtpAsync(string email, string token, string newPassword)
        {
            return Task.FromResult(new OperationResult(true, null));
        }

        public Task<SupabaseAuthResult> RefreshTokenAsync(string refreshToken)
        {
            return Task.FromResult(new SupabaseAuthResult(true, "Refreshed (stub)", null, "stub-access-token", "stub-refresh", DateTime.UtcNow.AddHours(1)));
        }

        public Task<(bool Success, JsonElement? UserData, string? ErrorMessage)> GetCurrentUserAsync(string accessToken)
        {
            var json = JsonDocument.Parse("{\"email\": \"stub@example.com\", \"user_metadata\": { \"full_name\": \"Stub User\" }}");
            return Task.FromResult<(bool, JsonElement?, string?)>((true, json.RootElement, null));
        }

        public Task<OperationResult> UpdateUserAsync(string accessToken, string? email = null, string? fullName = null, string? contact = null, string? avatarUrl = null)
        {
            return Task.FromResult(new OperationResult(true, null));
        }

        public string GetPublicFileUrl(string filePath)
        {
            return $"/stub-files/{Uri.EscapeDataString(filePath)}";
        }

        public Task<(bool Success, string? SignedUrl, string? ErrorMessage)> GetSignedFileUrlAsync(string filePath, string accessToken, int expirationSeconds = 2592000)
        {
            string? signedUrl = GetPublicFileUrl(filePath);
            return Task.FromResult((true, signedUrl, (string?)null));
        }

        public Task<OperationResult> UploadFileAsync(string filePath, Stream data, string accessToken, string contentType)
        {
            return Task.FromResult(new OperationResult(true, null));
        }

        public Task<OperationResult> SaveUserRecordAsync(string userId, string email, string filePath, string fileName, string suggestions, int? atsScore, string accessToken)
        {
            return Task.FromResult(new OperationResult(true, null));
        }

        public Task<(bool Success, List<T> Records, string? ErrorMessage)> GetUserRecordsAsync<T>(string email, string accessToken)
        {
            return Task.FromResult((true, new List<T>(), (string?)null));
        }

        public Task<(bool Success, byte[] Content, string ContentType, string? ErrorMessage)> DownloadFileAsync(string filePath, string accessToken)
        {
            return Task.FromResult((true, Array.Empty<byte>(), "application/pdf", (string?)null));
        }

        public Task<OperationResult> DeleteUserRecordAsync(string id, string accessToken)
        {
            return Task.FromResult(new OperationResult(true, null));
        }
    }
}