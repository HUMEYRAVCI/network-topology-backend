using System;
using System.ComponentModel.DataAnnotations;

namespace NetworkTopologyTracker.Api.Entities
{
    // Kullanıcılar ve Yetkilendirme Katmanı
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        public string Password { get; set; } = string.Empty; // Güvenli şekilde hash'lenmiş şifre

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "Viewer"; // "SuperAdmin" veya "Viewer"

        public DateTime? LastLoginAt { get; set; }
    }
}