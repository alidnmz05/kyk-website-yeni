// Models/Admin.cs
namespace YurtMenu.API.Models
{
    public class Admin
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty; // SHA256 veya başka bir hash kullanacağız

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
