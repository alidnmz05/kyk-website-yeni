// Helpers/PasswordHasher.cs
using System.Security.Cryptography;
using System.Text;

namespace YurtMenu.API.Helpers
{
    public static class PasswordHasher
    {
        public static string ComputeSha256Hash(string rawData)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }
}
