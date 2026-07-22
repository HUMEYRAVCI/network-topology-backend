using System;
using System.ComponentModel.DataAnnotations;

namespace NetworkTopologyTracker.Api.Entities
{
    // Sistem Denetim ve Log Kayıtları Katmanı
    public class SystemLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty; // İşlemi yapan kullanıcı

        [Required]
        [MaxLength(150)]
        public string Action { get; set; } = string.Empty; // Örn: "Device_Created", "User_Login"

        [MaxLength(1000)]
        public string? Details { get; set; } // Örn: "1004 ID'li cihaz güncellendi."

        [Required]
        [MaxLength(50)]
        public string ActionType { get; set; } = string.Empty; // Örn: "INFO", "WARNING", "ERROR"

        public DateTime Timestamp { get; set; } = DateTime.UtcNow; // İşlem zamanı
    }
}