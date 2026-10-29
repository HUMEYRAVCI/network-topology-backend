using System;
using System.ComponentModel.DataAnnotations;

namespace NetworkTopologyTracker.Api.Entities
{
    /// <summary>
    /// Kimlik Doğrulama ve Yetkilendirme Katmanı: Sistem Kullanıcısı (User) Varlığı.
    /// Uygulamaya giriş yapabilen personelin kimlik bilgilerini, kriptografik parola özetini (Hash)
    /// ve sistem içindeki yetki seviyesini (Role) temsil eder.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Kullanıcının veritabanındaki benzersiz birincil anahtarı (Primary Key - Identity).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Kullanıcının adı (Örn: "Hümeyra"). JWT claim'lerinde ve işlem loglarında kullanılır.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Kullanıcının soyadı (Örn: "Avcı").
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Sisteme girişte kullanılan benzersiz kullanıcı adı (Örn: "humeyra", "ahmet_viewer").
        /// AppDbContext üzerinde Unique Index ile veritabanı seviyesinde tekilliği garanti altına alınmıştır.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Kullanıcının parolasının <c>PasswordHasher&lt;User&gt;</c> (PBKDF2) ile hash'lenmiş hali.
        /// Güvenlik gereği düz metin (plain-text) şifre asla veritabanında saklanmaz.
        /// </summary>
        [Required]
        [MaxLength(250)]
        public string Password { get; set; } = string.Empty; // Güvenli şekilde hash'lenmiş şifre

        /// <summary>
        /// Kullanıcının Rol Tabanlı Erişim Kontrolü (RBAC) yetki seviyesi.
        /// Desteklenen roller:
        /// - "SuperAdmin": Tüm CRUD (Ekleme/Güncelleme/Silme) işlemlerini yapabilir, cihaz şifrelerini ve logları görebilir.
        /// - "Viewer": Sadece topoloji haritasını ve envanteri salt-okunur (Read-Only) görüntüleyebilir; hassas bilgileri göremez.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "Viewer"; // "SuperAdmin" veya "Viewer"

        /// <summary>
        /// Kullanıcının sisteme en son başarılı giriş yaptığı tarih ve saat (Türkiye Saati - UTC+3).
        /// Kullanıcı henüz hiç giriş yapmadıysa null değerini alır.
        /// </summary>
        public DateTime? LastLoginAt { get; set; }
    }
}