using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetworkTopologyTracker.Api.Entities
{
    // Hiyerarşinin 2. Basamağı: Tesisin İçindeki Odalar / Katlar (Örn: Sistem Odası, Kat 1)
    public class Room
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? FloorInfo { get; set; } // Kat bilgisi

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Üst Kademeye Bağlantı: Bu oda hangi tesise ait?
        [Required]
        public int FacilityId { get; set; }
        
        [ForeignKey("FacilityId")]
        public Facility? Facility { get; set; }

        // Alt Kademeye Bağlantı: Bu odanın içinde hangi cihazlar var?
        public ICollection<Device> Devices { get; set; } = new List<Device>();
    }
}