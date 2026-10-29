using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NetworkTopologyTracker.Api.Entities
{
    /// <summary>
    /// Ağ Topolojisi Hiyerarşisinin 2. Basamağı: Kat (Floor) Varlığı.
    /// Bir tesis (Facility) içerisindeki fiziksel katları temsil eder ve odaları (Room) bünyesinde barındırır.
    /// Hiyerarşi: Facility (Bina) -> Floor (Kat) -> Room (Oda) -> Device (Cihaz) -> Port -> Connection (Bağlantı).
    /// </summary>
    public class Floor
    {
        /// <summary>
        /// Kat kaydının veritabanındaki benzersiz birincil anahtarı (Primary Key - Identity).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Katın ekranda gösterilecek adı (Örn: "Zemin Kat", "1. Kat", "2. Kat (Meclis)").
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // "Zemin Kat", "1. Kat", "Açık Alan"

        /// <summary>
        /// Katların arayüzde ve sorgularda doğru sırayla listelenmesi için kullanılan sayısal seviye değeri
        /// (Örn: -1 = Bodrum Kat, 0 = Zemin Kat, 1 = 1. Kat, 2 = 2. Kat).
        /// </summary>
        public int Level { get; set; } = 0; // Sıralama kolaylığı için kat numarası (0: Zemin, 1: 1. Kat, -1: Bodrum)

        /// <summary>
        /// Üst Kademeye Bağlantı (Foreign Key): Bu katın ait olduğu binanın (Facility) ID değeri.
        /// </summary>
        [Required]
        public int FacilityId { get; set; }

        /// <summary>
        /// Çoka-Bir (N-1) Navigation Property: Katın bağlı olduğu üst bina nesnesi.
        /// [JsonIgnore] özniteliği, serileştirme (serialization) sırasında Floor -> Facility -> Floors
        /// şeklinde oluşabilecek sonsuz döngüyü (Circular Reference) engellemek için kullanılır.
        /// </summary>
        [ForeignKey("FacilityId")]
        [JsonIgnore] // Floor -> Facility -> Floors sonsuz JSON döngüsünü engeller
        public Facility? Facility { get; set; }

        /// <summary>
        /// Bire-Çok (1-N) Navigation Property: Bu katta yer alan odaların / birimlerin (Room) koleksiyonu.
        /// Kat silindiğinde altındaki odalar Cascade Delete kuralı ile otomatik olarak silinir.
        /// </summary>
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}