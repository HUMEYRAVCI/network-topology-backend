using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NetworkTopologyTracker.Api.Entities
{
    /// <summary>
    /// Ağ Topolojisi Hiyerarşisinin 6. Basamağı (Bağlantı Katmanı): Fiziksel / Mantıksal Ağ Bağlantısı (Connection) Varlığı.
    /// Bir kaynak cihazın belirli bir portundan (SourcePort) çıkıp hedef bir ağ cihazına (TargetDevice) ulaşan
    /// kablolu (Fiber, Cat6) veya kablosuz (Wireless) bağlantıyı temsil eder.
    /// Topoloji haritasındaki düğümler (Node) arasındaki kenarları (Edge/Link) oluşturur.
    /// </summary>
    public class Connection
    {
        /// <summary>
        /// Bağlantı kaydının veritabanındaki benzersiz birincil anahtarı (Primary Key - Identity).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// İletim ortamının / kablonun türü (Örn: "Fiber Optic", "Fiber", "Cat6", "Wireless").
        /// Varsayılan olarak "Fiber" atanır.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ConnectionType { get; set; } = "Fiber"; // Fiber, Cat6, Wireless

        /// <summary>
        /// Bağlantı kaydının oluşturulma tarihi ve saati.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Kaynak Port Yabancı Anahtarı (Foreign Key): Bağlantının çıktığı fiziksel portun (Port) ID değeri.
        /// </summary>
        [Required]
        public int SourcePortId { get; set; }

        /// <summary>
        /// Bire-Bir (1-1) Navigation Property: Bağlantının başladığı kaynak port nesnesi.
        /// MS SQL Server'da çoklu kaskad yollarını (Multiple Cascade Paths) önlemek amacıyla
        /// AppDbContext içerisinde OnDelete(DeleteBehavior.Restrict) olarak yapılandırılmıştır.
        /// </summary>
        [ForeignKey("SourcePortId")]
        [JsonIgnore] // JSON döngülerini (circular reference) engeller
        public Port? SourcePort { get; set; }

        /// <summary>
        /// Hedef Cihaz Yabancı Anahtarı (Foreign Key): Bağlantının sonlandığı hedef cihazın (Device) ID değeri.
        /// </summary>
        [Required]
        public int TargetDeviceId { get; set; }

        /// <summary>
        /// Çoka-Bir (N-1) Navigation Property: Bağlantının ulaştığı hedef cihaz nesnesi (Port-to-Device topoloji modeli).
        /// MS SQL Server'da döngüsel kaskad hatasını önlemek için DeleteBehavior.Restrict olarak ayarlanmıştır.
        /// </summary>
        [ForeignKey("TargetDeviceId")]
        [JsonIgnore] // JSON döngülerini engeller
        public Device? TargetDevice { get; set; }
    }
}