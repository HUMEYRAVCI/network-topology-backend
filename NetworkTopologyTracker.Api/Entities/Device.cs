using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
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

        // ÖNEMLİ: Varsayılan değer ("Switch") KASITLI olarak kaldırıldı.
        // Bu alan artık DTO seviyesinde (TopologyController.DeviceRequest) enum tabanlı
        // olarak zorunlu kılınıyor; kullanıcı seçim yapmazsa istek 400 Bad Request ile reddedilir.
        // Burada [Required] bırakılması, entity'nin herhangi bir yoldan (örn. ileride
        // eklenecek başka bir servis/import akışı) boş DeviceType ile kaydedilmesini de
        // veritabanı/model seviyesinde engellemek için savunma katmanı olarak duruyor.
        [Required(ErrorMessage = "Cihaz türü boş olamaz.")]
        [MaxLength(50)]
        public string DeviceType { get; set; } = string.Empty; // Router, Switch, Access Point, Firewall, Server
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Active"; // Active, Inactive, Maintenance
        [MaxLength(150)]
        public string? Model { get; set; } // Marka ve Model Tek Alanda (Örn: Cisco Catalyst 2960)
        [MaxLength(100)]
        public string? MacAddress { get; set; }
        [MaxLength(100)]
        public string? SerialNumber { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // Üst Kademeye Bağlantı: Bu cihaz hangi odada duruyor?
        [Required]
        public int RoomId { get; set; }
        [ForeignKey("RoomId")]
        [JsonIgnore] // 🛑 Cihaz -> Oda -> Cihazlar sonsuz JSON döngüsünü engeller
        public Room? Room { get; set; }
        // Alt Kademeye Bağlantı: Bu cihazın üzerindeki fiziksel portlar
        public ICollection<Port> Ports { get; set; } = new List<Port>();
        // 1-1 Gizlilik İlişkisi: Hassas giriş bilgileri (Sadece yetkili görebilir)
        public Credential? Credential { get; set; }
        // Bu cihaza gelen kablo bağlantıları (Connection.cs ile ters yönlü ilişki)
        [JsonIgnore]
        public ICollection<Connection> IncomingConnections { get; set; } = new List<Connection>();
    }

    // ==================== CİHAZ TÜRÜ ENUM'U ====================
    // Ayrı dosya açmak yerine burada tutuluyor; sadece Device ile ilgili olduğu için
    // ekstra dosya kalabalığı yaratmaya gerek yok. DTO seviyesinde (TopologyController)
    // katı validasyon için kullanılıyor.
    public enum NetworkDeviceType
    {
        Router,
        Switch,
        AccessPoint,
        Firewall,
        Server
    }

    // Enum <-> veritabanında/response'larda kullanılan görünen string (Örn: "Access Point")
    // arasında TEK ve tutarlı bir dönüşüm noktası. Seed verisiyle birebir eşleşir.
    public static class NetworkDeviceTypeExtensions
    {
        private static readonly Dictionary<NetworkDeviceType, string> DisplayNames = new()
        {
            { NetworkDeviceType.Router, "Router" },
            { NetworkDeviceType.Switch, "Switch" },
            { NetworkDeviceType.AccessPoint, "Access Point" },
            { NetworkDeviceType.Firewall, "Firewall" },
            { NetworkDeviceType.Server, "Server" }
        };

        public static string ToDisplayString(this NetworkDeviceType type)
        {
            return DisplayNames.TryGetValue(type, out var value) ? value : type.ToString();
        }

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