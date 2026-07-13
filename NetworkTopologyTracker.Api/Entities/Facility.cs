using System.ComponentModel.DataAnnotations;

namespace NetworkTopologyTracker.Api.Entities
{
    // Hiyerarşinin 1. Basamağı: Tesis / Bina (Örn: Talas Belediyesi Merkez Kütüphanesi)
    public class Facility
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Address { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // İlişki: Bir tesisin içinde birden fazla oda bulunabilir.
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}