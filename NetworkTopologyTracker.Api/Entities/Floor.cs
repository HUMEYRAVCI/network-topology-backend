using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NetworkTopologyTracker.Api.Entities
{
    public class Floor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // "Zemin Kat", "1. Kat", "Açık Alan"

        public int Level { get; set; } = 0; // Sıralama kolaylığı için kat numarası (0: Zemin, 1: 1. Kat, -1: Bodrum)

        // Üst Kademeye Bağlantı: Her kat sadece bir binaya (Facility) aittir.
        [Required]
        public int FacilityId { get; set; }

        [ForeignKey("FacilityId")]
        [JsonIgnore] // 🛑 Floor -> Facility -> Floors sonsuz JSON döngüsünü engeller
        public Facility? Facility { get; set; }

        // Alt Kademeye Bağlantı: Bir katta birden fazla oda (Room) yer alabilir.
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}