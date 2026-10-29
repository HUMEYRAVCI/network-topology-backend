using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NetworkTopologyTracker.Api.Entities
{
    /// <summary>
    /// Güvenlik ve Erişim Katmanı: Cihaz Kimlik Bilgileri (Credential) Varlığı.
    /// Ağ cihazlarının yönetim arayüzlerine (SSH, Telnet, Web GUI vb.) bağlanmak için gereken
    /// IP adresi, yönetici kullanıcı adı ve parola bilgilerini ayrı bir tabloda (1-1 ilişkiyle) tutar.
    /// GÜVENLİK NOTU: Bu tablodaki veriler TopologyController.BuildSecurityInfo() metodu aracılığıyla
    /// yalnızca "SuperAdmin" rolüne sahip kullanıcılara açık şekilde döndürülür; "Viewer" rolündeki
    /// kullanıcılar için "GİZLİ (Yetki Yok)" olarak maskelenir.
    /// </summary>
    public class Credential
    {
        /// <summary>
        /// Kimlik bilgisi kaydının veritabanındaki benzersiz birincil anahtarı (Primary Key - Identity).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Cihazın yerel ağdaki yönetim IPv4 adresi (Örn: "192.168.1.10").
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string IpAddress { get; set; } = "0.0.0.0";

        /// <summary>
        /// Cihaz yönetim paneline giriş için kullanılan yönetici kullanıcı adı (Örn: "admin", "sysadmin").
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Cihaz yönetim paneline giriş için kullanılan parola bilgisi.
        /// </summary>
        [Required]
        [MaxLength(250)]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Bire-Bir (1-1) Yabancı Anahtar (Foreign Key): Bu erişim bilgilerinin ait olduğu cihazın (Device) ID değeri.
        /// </summary>
        [Required]
        public int DeviceId { get; set; }

        /// <summary>
        /// Bire-Bir (1-1) Navigation Property: Kimlik bilgisinin bağlı olduğu cihaz nesnesi.
        /// [JsonIgnore] ile hem sonsuz JSON döngüsü (Circular Reference) hem de dolaylı veri sızıntısı önlenir.
        /// </summary>
        [ForeignKey("DeviceId")]
        [JsonIgnore] // JSON döngüsünü (Circular Reference) ve yetkisiz veri sızıntısını engeller
        public Device? Device { get; set; }
    }
}