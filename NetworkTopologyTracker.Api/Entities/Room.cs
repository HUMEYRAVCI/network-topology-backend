using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NetworkTopologyTracker.Api.Entities
{
    /// <summary>
    /// Ağ Topolojisi Hiyerarşisinin 3. Basamağı: Oda / Birim (Room) Varlığı.
    /// Bir katın (Floor) içerisindeki fiziksel odaları, müdürlük ofislerini, sistem odalarını veya salonları temsil eder.
    /// Ağ cihazları (Device) doğrudan bir odaya konumlandırılır.
    /// Hiyerarşi: Facility (Bina) -> Floor (Kat) -> Room (Oda) -> Device (Cihaz) -> Port -> Connection (Bağlantı).
    /// </summary>
    public class Room
    {
        /// <summary>
        /// Oda kaydının veritabanındaki benzersiz birincil anahtarı (Primary Key - Identity).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Odanın veya idari birimin adı (Örn: "Ana Bilgi İşlem & Sistem Odası", "Yazı İşleri Müdürlüğü").
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Oda kaydının veritabanında oluşturulma tarihi ve saati.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Üst Kademeye Bağlantı (Foreign Key): Bu odanın bulunduğu katın (Floor) ID değeri.
        /// </summary>
        [Required]
        public int FloorId { get; set; }

        /// <summary>
        /// Çoka-Bir (N-1) Navigation Property: Odanın bağlı olduğu kat nesnesi.
        /// [JsonIgnore] özniteliği, Room -> Floor -> Rooms sonsuz JSON serileştirme döngüsünü önler.
        /// </summary>
        [ForeignKey("FloorId")]
        [JsonIgnore] // Room -> Floor -> Rooms sonsuz JSON döngüsünü engeller
        public Floor? Floor { get; set; }

        /// <summary>
        /// Bire-Çok (1-N) Navigation Property: Bu odanın içerisine konumlandırılmış ağ ve uç cihazların (Device) listesi.
        /// Oda silindiğinde içindeki cihazlar Cascade Delete kuralı ile otomatik silinir.
        /// </summary>
        public ICollection<Device> Devices { get; set; } = new List<Device>();
    }
}