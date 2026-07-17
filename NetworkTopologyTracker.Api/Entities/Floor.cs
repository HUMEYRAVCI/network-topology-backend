using System.Collections.Generic;

namespace NetworkTopologyTracker.Api.Entities
{
    public class Floor
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // "Zemin Kat", "1. Kat", "Açık Alan"
        public int Level { get; set; } // Sıralama kolaylığı için kat numarası (0: Zemin, 1: 1. Kat, -1: Bodrum)

        // İlişki: Her kat sadece bir binaya (Facility) aittir.
        public int FacilityId { get; set; }
        public Facility Facility { get; set; } = null!;

        // İlişki: Bir katta birden fazla oda (Room) yer alabilir.
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}