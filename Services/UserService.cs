using System.Security.Cryptography;
using System.Text;

namespace SmartResumeAnalyzer.Services
{
    // Very small demo user store. Not for production use.
    public class UserService
    {
        private readonly Dictionary<string, string> _users = new();

        public bool Register(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            if (_users.ContainsKey(username))
                return false;

            _users[username] = Hash(password);
            return true;
        }

        public bool ValidateCredentials(string username, string password)
        {
            if (!_users.TryGetValue(username, out var stored))
                return false;

            return stored == Hash(password);
        }

        private static string Hash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }
    }
}

