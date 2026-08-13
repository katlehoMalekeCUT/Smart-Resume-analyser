using System.Text.Json;

namespace SmartResumeAnalyzer.Services
{
    public record SupabaseAuthResult(bool Success, string? Message, string? Email, string? AccessToken, string? RefreshToken, DateTime? ExpiresAt);
    public record OperationResult(bool Success, string? ErrorMessage);

    public interface ISupabaseAuthService
    {
        Task<SupabaseAuthResult> RegisterAsync(string email, string password);
        Task<SupabaseAuthResult> LoginAsync(string email, string password);
        Task<SupabaseAuthResult> RefreshTokenAsync(string refreshToken);
        Task<SupabaseAuthResult> VerifyOtpAsync(string email, string token, string type = "signup");
        Task<OperationResult> ForgotPasswordAsync(string email);
        Task<OperationResult> ResetPasswordWithOtpAsync(string email, string token, string newPassword);
        Task<(bool Success, JsonElement? UserData, string? ErrorMessage)> GetCurrentUserAsync(string accessToken);
        Task<OperationResult> UpdateUserAsync(string accessToken, string? email = null, string? fullName = null, string? contact = null, string? avatarUrl = null);
        string GetPublicFileUrl(string filePath);
        Task<(bool Success, string? SignedUrl, string? ErrorMessage)> GetSignedFileUrlAsync(string filePath, string accessToken, int expirationSeconds = 2592000);
        Task<OperationResult> UploadFileAsync(string filePath, Stream data, string accessToken, string contentType);
        Task<OperationResult> SaveUserRecordAsync(string userId, string email, string filePath, string fileName, string suggestions, int? atsScore, string accessToken);
        Task<(bool Success, List<T> Records, string? ErrorMessage)> GetUserRecordsAsync<T>(string email, string accessToken);
        Task<(bool Success, byte[] Content, string ContentType, string? ErrorMessage)> DownloadFileAsync(string filePath, string accessToken);
        Task<OperationResult> DeleteUserRecordAsync(string id, string accessToken);
    }
}