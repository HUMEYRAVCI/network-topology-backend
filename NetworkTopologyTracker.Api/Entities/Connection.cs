using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NetworkTopologyTracker.Api.Entities
{
    // Hiyerarşinin Bağlantı Katmanı: Cihazlar veya Portlar arası kablo bağlantı haritası
    public class Connection
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ConnectionType { get; set; } = "Fiber"; // Fiber, Cat6, Wireless

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Kablonun Çıktığı Yer: Hangi kaynak porttan çıkıyor?
        [Required]
        public int SourcePortId { get; set; }

        [ForeignKey("SourcePortId")]
        [JsonIgnore] // JSON döngülerini (circular reference) engeller
        public Port? SourcePort { get; set; }

        // Kablonun Bittiği Yer (Port-to-Device ilişkisi için mevcut yapın)
        [Required]
        public int TargetDeviceId { get; set; }

        [ForeignKey("TargetDeviceId")]
        [JsonIgnore] // JSON döngülerini engeller
        public Device? TargetDevice { get; set; }
    }
}