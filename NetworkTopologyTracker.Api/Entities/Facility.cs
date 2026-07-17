using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetworkTopologyTracker.Api.Entities
{
    // Hiyerarşinin 2. Basamağı: Tesis / Bina (Örn: Talas Belediyesi Merkez Kütüphanesi)
    public class Facility
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        // Talas Belediyesi'nin bizden istediği bina tiplerini (Merkez Bina, Kütüphane, Müze vb.) burada tutacağız.
        [Required]
        [MaxLength(100)]
        public string FacilityType { get; set; } = string.Empty; 

        [MaxLength(250)]
        public string? Address { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Üst Kademeye Bağlantı: Bu bina hangi kuruma ait?
        [Required]
        public int InstitutionId { get; set; }

        [ForeignKey("InstitutionId")]
        public Institution? Institution { get; set; }

        // Alt Kademeye Bağlantı: Bir tesisin içinde birden fazla Kat bulunabilir.
        public ICollection<Floor> Floors { get; set; } = new List<Floor>();
    }
}