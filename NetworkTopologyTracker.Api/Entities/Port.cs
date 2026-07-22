using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NetworkTopologyTracker.Api.Entities
{
    // Hiyerarşinin 4. Basamağı: Cihazın Üzerindeki Fiziksel Portlar (Örn: GigabitEthernet0/1, Port 24)
    public class Port
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string PortNumber { get; set; } = string.Empty; // Port adı veya numarası

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Connected"; // Connected, Disconnected, Faulty

        // Üst Kademeye Bağlantı: Bu port hangi cihaza ait?
        [Required]
        public int DeviceId { get; set; }

        [ForeignKey("DeviceId")]
        [JsonIgnore] // 🛑 Port -> Device -> Ports sonsuz JSON döngüsünü engeller
        public Device? Device { get; set; }

        // Topoloji Bağlantısı: Bu porttan başka bir yere giden aktif bir kablo bağlantısı
        [JsonIgnore] // 🛑 Port -> Connection -> Port sonsuz JSON döngüsünü engeller
        public Connection? Connection { get; set; }
    }
}