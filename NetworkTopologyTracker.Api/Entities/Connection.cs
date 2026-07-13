using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetworkTopologyTracker.Api.Entities
{
    // Hiyerarşinin Bağlantı Katmanı: Cihazlar arası kablo bağlantı haritası
    public class Connection
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ConnectionType { get; set; } = "Fiber"; // Fiber, Cat6, Wireless

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Kablonun Çıktığı Yer: Hangi cihazın, hangi portundan kablo çıkıyor?
        [Required]
        public int SourcePortId { get; set; }

        [ForeignKey("SourcePortId")]
        public Port? SourcePort { get; set; }

        // Kablonun Gittiği Yer: Kablo hangi hedef cihaza bağlanıyor?
        [Required]
        public int TargetDeviceId { get; set; }

        [ForeignKey("TargetDeviceId")]
        public Device? TargetDevice { get; set; }
    }
}