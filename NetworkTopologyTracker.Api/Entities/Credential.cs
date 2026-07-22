using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NetworkTopologyTracker.Api.Entities
{
    // Güvenlik Katmanı: Cihazların Hassas Giriş Bilgileri (Sadece yetkili kullanıcılar görebilir)
    public class Credential
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string IpAddress { get; set; } = "0.0.0.0";

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        public string Password { get; set; } = string.Empty;

        // 1-1 Eşleşme: Bu şifre seti hangi cihaza ait?
        [Required]
        public int DeviceId { get; set; }

        [ForeignKey("DeviceId")]
        [JsonIgnore] // 🛑 JSON döngüsünü (Circular Reference) ve yetkisiz veri sızıntısını engeller
        public Device? Device { get; set; }
    }
}