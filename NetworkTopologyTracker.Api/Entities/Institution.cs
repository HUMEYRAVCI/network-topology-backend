using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NetworkTopologyTracker.Api.Entities
{
    // Hiyerarşinin En Üst Katmanı (Kök): Kurum (Örn: Talas Belediyesi)
    public class Institution
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty; // "Talas Belediyesi"

        [MaxLength(500)]
        public string? Description { get; set; }

        // Alt Kademeye Bağlantı: Bir kurumun birden fazla binası/tesisi (Facility) olabilir.
        public ICollection<Facility> Facilities { get; set; } = new List<Facility>();
    }
}