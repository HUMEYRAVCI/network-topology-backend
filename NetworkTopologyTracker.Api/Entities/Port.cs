using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NetworkTopologyTracker.Api.Entities
{
    /// <summary>
    /// Ağ Topolojisi Hiyerarşisinin 5. Basamağı: Fiziksel Ağ Portu (Port) Varlığı.
    /// Bir ağ cihazının (Device) üzerindeki fiziksel veya mantıksal ağ arayüzlerini (Örn: "Gi0/1", "WAN1", "Eth0")
    /// ve bu portların doluluk/hat durumunu temsil eder.
    /// Hiyerarşi: Facility (Bina) -> Floor (Kat) -> Room (Oda) -> Device (Cihaz) -> Port -> Connection (Bağlantı).
    /// </summary>
    public class Port
    {
        /// <summary>
        /// Portun veritabanındaki benzersiz birincil anahtarı (Primary Key - Identity).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Cihaz üzerindeki portun arayüz etiketi veya numarası (Örn: "Gi0/0/1", "Gi0/1", "LAN1", "NIC1", "Eth0").
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string PortNumber { get; set; } = string.Empty; // Port adı veya numarası

        /// <summary>
        /// Portun fiziksel hat durumu (Örn: "Connected" = Kablo takılı/aktif, "Disconnected" = Boşta, "Faulty" = Arızalı).
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Connected"; // Connected, Disconnected, Faulty

        /// <summary>
        /// Üst Kademeye Bağlantı (Foreign Key): Bu portun üzerinde bulunduğu cihazın (Device) ID değeri.
        /// </summary>
        [Required]
        public int DeviceId { get; set; }

        /// <summary>
        /// Çoka-Bir (N-1) Navigation Property: Portun ait olduğu cihaz nesnesi.
        /// [JsonIgnore] ile Port -> Device -> Ports sonsuz JSON döngüsü engellenir.
        /// </summary>
        [ForeignKey("DeviceId")]
        [JsonIgnore] // Port -> Device -> Ports sonsuz JSON döngüsünü engeller
        public Device? Device { get; set; }

        /// <summary>
        /// Bire-Bir (1-1) Navigation Property: Bu porttan çıkarak başka bir hedef cihaza giden aktif bağlantı (Connection).
        /// Bir kaynak porttan aynı anda yalnızca bir fiziksel kablo çıkabilir.
        /// </summary>
        [JsonIgnore] // Port -> Connection -> Port sonsuz JSON döngüsünü engeller
        public Connection? Connection { get; set; }
    }
}