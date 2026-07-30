using System.ComponentModel.DataAnnotations;

namespace HajurKoCarRental.Models
{
    public class PendingRegistration
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Token { get; set; }
        [Required]
        public string Firstname { get; set; }
        [Required]
        public string Lastname { get; set; }
        [Required]
        public string Address { get; set; }
        [Required]
        public string Email { get; set; }
        public string? PasswordHash { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        public string? LoginProvider { get; set; }
        public string? ProviderKey { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
