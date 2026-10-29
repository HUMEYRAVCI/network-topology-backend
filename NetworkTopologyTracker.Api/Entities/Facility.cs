using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NetworkTopologyTracker.Api.Entities
{
    /// <summary>
    /// Ağ Topolojisi Hiyerarşisinin 1. Basamağı: Tesis / Bina (Facility) Varlığı.
    /// Kuruma bağlı fiziksel hizmet binalarını, kütüphaneleri, şantiyeleri veya sosyal tesisleri temsil eder.
    /// Hiyerarşi: Facility (Bina) -> Floor (Kat) -> Room (Oda) -> Device (Cihaz) -> Port -> Connection (Bağlantı).
    /// </summary>
    public class Facility
    {
        /// <summary>
        /// Tesisin veritabanındaki benzersiz birincil anahtarı (Primary Key - Identity).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Tesisin / binanın resmi adı (Örn: "Talas Belediyesi Merkez Binası").
        /// Zorunlu alandır ve en fazla 150 karakter uzunluğunda olabilir.
        /// </summary>
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Tesisin kullanım amacını / kategorisini belirten tür bilgisi
        /// (Örn: "Merkez Hizmet Binası", "Kültür Merkezi", "Semt Kütüphanesi", "Sosyal Tesis").
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string FacilityType { get; set; } = string.Empty; 

        /// <summary>
        /// Tesisin açık fiziksel adres bilgisi (İsteğe bağlı / Nullable, maksimum 250 karakter).
        /// </summary>
        [MaxLength(250)]
        public string? Address { get; set; }

        /// <summary>
        /// Tesis kaydının veritabanında oluşturulma tarihi ve saati.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Bire-Çok (1-N) Navigation Property: Bir binanın bünyesinde birden fazla kat (Floor) bulunabilir.
        /// Bina silindiğinde altındaki katlar Cascade Delete kuralı ile otomatik silinir.
        /// </summary>
        public ICollection<Floor> Floors { get; set; } = new List<Floor>();
    }
}