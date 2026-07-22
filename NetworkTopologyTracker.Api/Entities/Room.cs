using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NetworkTopologyTracker.Api.Entities
{
    // Hiyerarşinin 4. Basamağı: Katın İçindeki Odalar / Birimler (Örn: Bilgi İşlem, Başkanlık)
    public class Room
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Üst Kademeye Bağlantı: Bu oda hangi kata ait?
        [Required]
        public int FloorId { get; set; }

        [ForeignKey("FloorId")]
        [JsonIgnore] // 🛑 Room -> Floor -> Rooms sonsuz JSON döngüsünü engeller
        public Floor? Floor { get; set; }

        // Alt Kademeye Bağlantı: Bu odanın içinde hangi cihazlar var?
        public ICollection<Device> Devices { get; set; } = new List<Device>();
    }
}