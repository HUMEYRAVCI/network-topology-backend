using System;
using System.ComponentModel.DataAnnotations;

namespace NetworkTopologyTracker.Api.Entities
{
    /// <summary>
    /// Denetim İzi (Audit Trail) ve Sistem Log Katmanı: <see cref="SystemLog"/> Varlığı.
    /// Sistemde gerçekleştirilen kritik işlemlerin (Kullanıcı girişi, bina/kat/oda/cihaz ekleme,
    /// güncelleme ve silme operasyonları) kim tarafından, ne zaman ve hangi detaylarla yapıldığını
    /// kayıt altına alır. Siber güvenlik ve izlenebilirlik (traceability) gereksinimlerini karşılar.
    /// </summary>
    public class SystemLog
    {
        /// <summary>
        /// Log kaydının veritabanındaki benzersiz birincil anahtarı (Primary Key - Identity).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// İşlemi gerçekleştiren kullanıcının kullanıcı adı (JWT Token içindeki ClaimTypes.Name'den alınır).
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty; // İşlemi yapan kullanıcı

        /// <summary>
        /// Gerçekleştirilen eylemin kısa başlığı (Örn: "Kullanıcı Girişi", "Bina Eklendi", "Cihaz Silindi").
        /// </summary>
        [Required]
        [MaxLength(150)]
        public string Action { get; set; } = string.Empty; // Örn: "Device_Created", "User_Login"

        /// <summary>
        /// İşlemin kapsamını açıklayan detaylı metin (Örn: "Silinen öğe adı, ID'si ve altındaki etkilenen kayıt sayıları").
        /// </summary>
        [MaxLength(1000)]
        public string? Details { get; set; } // Örn: "1004 ID'li cihaz güncellendi."

        /// <summary>
        /// İşlemin kategorisi / türü (Örn: "Giriş", "Ekleme", "Güncelleme", "Silme").
        /// Arayüzde logların filtrelenmesi ve renklendirilmesi için kullanılır.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ActionType { get; set; } = string.Empty; // Örn: "INFO", "WARNING", "ERROR"

        /// <summary>
        /// İşlemin gerçekleştiği tarih ve saat bilgisi.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow; // İşlem zamanı
    }
}