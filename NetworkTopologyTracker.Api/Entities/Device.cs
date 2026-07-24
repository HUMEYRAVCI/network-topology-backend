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

        [Required]
        [MaxLength(50)]
        public string DeviceType { get; set; } = "Switch"; // Router, Switch, AP, Server

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
}