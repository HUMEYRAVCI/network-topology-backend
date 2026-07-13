using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetworkTopologyTracker.Api.Entities
{
    // Hiyerarşinin 3. Basamağı: Odadaki Cihazlar (Router, Switch, Access Point, Sunucu vb.)
    public class Device
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string DeviceType { get; set; } = "Switch"; // Router, Switch, AP, Server

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Active"; // Active, Inactive, Maintenance

        [MaxLength(100)]
        public string? Brand { get; set; } // Marka (Örn: Cisco)

        [MaxLength(100)]
        public string? Model { get; set; } // Model (Örn: Catalyst 2960)

        [MaxLength(100)]
        public string? MacAddress { get; set; }

        [MaxLength(100)]
        public string? SerialNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Üst Kademeye Bağlantı: Bu cihaz hangi odada duruyor?
        [Required]
        public int RoomId { get; set; }

        [ForeignKey("RoomId")]
        public Room? Room { get; set; }

        // Alt Kademeye Bağlantı: Bu cihazın üzerindeki fiziksel portlar
        public ICollection<Port> Ports { get; set; } = new List<Port>();

        // 1-1 Gizlilik İlişkisi: Hassas giriş bilgileri (Sadece yetkili görebilir)
        // Cihaz silindiğinde şifresi de veritabanından otomatik silinecek.
        public Credential? Credential { get; set; }
    }
}