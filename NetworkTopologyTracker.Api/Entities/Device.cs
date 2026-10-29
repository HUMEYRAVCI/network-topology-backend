using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace NetworkTopologyTracker.Api.Entities
{
    /// <summary>
    /// Ağ Topolojisi Hiyerarşisinin 4. Basamağı: Ağ veya Uç Cihaz (Device) Varlığı.
    /// Bir odada (Room) bulunan Router, Switch, Firewall, Access Point, Server, PC veya Printer gibi
    /// donanımları, bunların fiziksel özelliklerini (Model, MAC, Seri No) ve çalışma durumlarını temsil eder.
    /// Hiyerarşi: Facility (Bina) -> Floor (Kat) -> Room (Oda) -> Device (Cihaz) -> Port -> Connection (Bağlantı).
    /// </summary>
    public class Device
    {
        /// <summary>
        /// Cihazın veritabanındaki benzersiz birincil anahtarı (Primary Key - Identity).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Cihazın topoloji haritasında ve envanterde görünen sistem adı (Hostname)
        /// (Örn: "TALAS-CORE-SWITCH-01", "TALAS-MERKEZ-AP-GIRIS").
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Cihazın donanım kategorisi (Örn: "Router", "Switch", "Access Point", "Firewall", "Server", "PC", "Printer").
        /// MİMARİ NOT: Varsayılan değer ("Switch") kasıtlı olarak kaldırılmıştır.
        /// API üzerinden gelen isteklerde bu alan DTO seviyesinde (TopologyController.DeviceRequest)
        /// NetworkDeviceType enum'u ile zorunlu tutulur ve eksikse 400 Bad Request döner.
        /// Entity üzerindeki [Required] özniteliği ise veritabanı seviyesinde ikinci bir savunma katmanı sağlar.
        /// </summary>
        [Required(ErrorMessage = "Cihaz türü boş olamaz.")]
        [MaxLength(50)]
        public string DeviceType { get; set; } = string.Empty; // Router, Switch, Access Point, Firewall, Server

        /// <summary>
        /// Cihazın anlık operasyonel durumu (Örn: "Active", "Inactive", "Maintenance").
        /// Varsayılan olarak "Active" atanır.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Active"; // Active, Inactive, Maintenance

        /// <summary>
        /// Cihazın üretici marka ve model bilgisi (Örn: "Cisco Catalyst 9300 48-Port", "Fortinet FortiGate 100F").
        /// </summary>
        [MaxLength(150)]
        public string? Model { get; set; } // Marka ve Model Tek Alanda (Örn: Cisco Catalyst 2960)

        /// <summary>
        /// Cihazın fiziksel ağ arayüzüne ait 48-bitlik donanım (MAC) adresi (Örn: "00:1A:2C:3B:00:65").
        /// Arama uç noktasında (devices/search) cihaz bulmak için de kullanılır.
        /// </summary>
        [MaxLength(100)]
        public string? MacAddress { get; set; }

        /// <summary>
        /// Envanter takibi için kullanılan üretici seri numarası (Örn: "SN-98234101").
        /// </summary>
        [MaxLength(100)]
        public string? SerialNumber { get; set; }

        /// <summary>
        /// Cihaz kaydının sisteme eklenme tarihi ve saati.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Üst Kademeye Bağlantı (Foreign Key): Cihazın fiziksel olarak bulunduğu odanın (Room) ID değeri.
        /// </summary>
        [Required]
        public int RoomId { get; set; }

        /// <summary>
        /// Çoka-Bir (N-1) Navigation Property: Cihazın içinde bulunduğu oda nesnesi.
        /// [JsonIgnore] ile Device -> Room -> Devices sonsuz JSON döngüsü engellenir.
        /// </summary>
        [ForeignKey("RoomId")]
        [JsonIgnore] // Cihaz -> Oda -> Cihazlar sonsuz JSON döngüsünü engeller
        public Room? Room { get; set; }

        /// <summary>
        /// Bire-Çok (1-N) Navigation Property: Cihazın üzerinde bulunan fiziksel ağ portlarının (Port) listesi
        /// (Örn: Gi0/1, WAN1, LAN0, Eth0). Cihaz silindiğinde portları da Cascade olarak silinir.
        /// </summary>
        public ICollection<Port> Ports { get; set; } = new List<Port>();

        /// <summary>
        /// Bire-Bir (1-1) Navigation Property: Cihazın yönetim arayüzüne (SSH/Web/Winbox) erişmek için kullanılan
        /// hassas kimlik ve IP bilgileri (Credential). Rol tabanlı yetkilendirme ile yalnızca SuperAdmin rolüne açılır.
        /// </summary>
        public Credential? Credential { get; set; }

        /// <summary>
        /// Bire-Çok (1-N) Ters Navigation Property: Başka cihazların portlarından çıkıp bu cihaza sonlanan
        /// fiziksel/kablosuz bağlantıların (Connection) listesi.
        /// </summary>
        [JsonIgnore]
        public ICollection<Connection> IncomingConnections { get; set; } = new List<Connection>();
    }

    /// <summary>
    /// API üzerinden yeni cihaz eklerken veya güncellerken kullanılan temel ağ cihazı türleri.
    /// DTO seviyesinde (DeviceRequest) tip güvenliği ve model doğrulaması (Validation) sağlamak amacıyla tanımlanmıştır.
    /// </summary>
    public enum NetworkDeviceType
    {
        Router,
        Switch,
        AccessPoint,
        Firewall,
        Server
    }

    /// <summary>
    /// <see cref="NetworkDeviceType"/> enum değerleri ile veritabanında ve JSON yanıtlarda kullanılan
    /// okunabilir metin (Display String, örn. "Access Point") arasında çift yönlü dönüşüm sağlayan yardımcı (Extension) sınıf.
    /// </summary>
    public static class NetworkDeviceTypeExtensions
    {
        /// <summary>
        /// Enum sabitlerini veritabanında saklanan standart metin karşılıklarıyla eşleştiren sözlük.
        /// Seed verilerindeki (DbInitializer) isimlendirmelerle birebir uyumludur.
        /// </summary>
        private static readonly Dictionary<NetworkDeviceType, string> DisplayNames = new()
        {
            { NetworkDeviceType.Router, "Router" },
            { NetworkDeviceType.Switch, "Switch" },
            { NetworkDeviceType.AccessPoint, "Access Point" },
            { NetworkDeviceType.Firewall, "Firewall" },
            { NetworkDeviceType.Server, "Server" }
        };

        /// <summary>
        /// Enum değerini veritabanına yazılacak okunabilir metin formatına dönüştürür.
        /// </summary>
        public static string ToDisplayString(this NetworkDeviceType type)
        {
            return DisplayNames.TryGetValue(type, out var value) ? value : type.ToString();
        }

        /// <summary>
        /// Metin formatındaki cihaz türünü büyük/küçük harf duyarsız olarak ilgili <see cref="NetworkDeviceType"/> enum değerine çevirir.
        /// </summary>
        public static bool TryParseDisplayString(string? value, out NetworkDeviceType result)
        {
            foreach (var kvp in DisplayNames)
            {
                if (string.Equals(kvp.Value, value, StringComparison.OrdinalIgnoreCase))
                {
                    result = kvp.Key;
                    return true;
                }
            }

            result = default;
            return false;
        }
    }
}