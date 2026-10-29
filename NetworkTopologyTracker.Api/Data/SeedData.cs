using Microsoft.EntityFrameworkCore;
using NetworkTopologyTracker.Api.Entities;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace NetworkTopologyTracker.Api.Data
{
    /// <summary>
    /// Kullanıcı Başlangıç Verisi Tohumlama Sınıfı (User Seeder).
    /// MİMARİ TASARIM KARARI (Single Responsibility Principle):
    /// Bu sınıf yalnızca sistem kullanıcılarını (<see cref="User"/>) veritabanına tohumlamakla sorumludur.
    /// Bina, kat, oda, cihaz, port ve bağlantı gibi topoloji varlıklarının tohumlama işlemi ise tamamen
    /// <see cref="DbInitializer"/> sınıfına devredilmiştir. Böylece iki farklı tohumlayıcı sınıfın aynı
    /// tablolar üzerinde çakışma veya mükerrer kayıt oluşturması önlenmiştir.
    /// </summary>
    public static class SeedData
    {
        /// <summary>
        /// Veritabanında henüz hiçbir kullanıcı yoksa varsayılan "SuperAdmin" ve "Viewer"
        /// hesaplarını oluşturur. Idempotent (tekrar çalıştırılabilir) yapıdadır; tabloda kayıt varsa dokunmaz.
        /// </summary>
        public static async Task Initialize(AppDbContext context)
        {
            // Users tablosu kontrol edilir; eğer en az bir kullanıcı mevcutsa tohumlama adımı atlanır.
            if (!await context.Users.AnyAsync())
            {
                // Parolaları düz metin yerine PBKDF2 algoritmasıyla güvenli şekilde özetlemek (Hash) için kullanılır.
                var hasher = new PasswordHasher<User>();

                // 1) Tam Yetkili Sistem Yöneticisi (SuperAdmin):
                // Tüm ekleme/silme/güncelleme işlemlerini yapabilir, cihaz şifrelerini ve denetim loglarını görebilir.
                var superAdmin = new User
                {
                    FirstName = "Hümeyra",
                    LastName = "Avcı",
                    Username = "humeyra",
                    Role = "SuperAdmin"
                };
                superAdmin.Password = hasher.HashPassword(superAdmin, "Password123!");

                // 2) Salt-Okunur İzleyici Kullanıcı (Viewer):
                // Topoloji haritasını ve cihazları görüntüleyebilir ancak değişiklik yapamaz ve şifreleri göremez.
                var viewer = new User
                {
                    FirstName = "Ahmet",
                    LastName = "Yılmaz",
                    Username = "ahmet_viewer",
                    Role = "Viewer"
                };
                viewer.Password = hasher.HashPassword(viewer, "Viewer123!");

                // Oluşturulan varsayılan kullanıcılar veritabanına kaydedilir.
                await context.Users.AddRangeAsync(superAdmin, viewer);
                await context.SaveChangesAsync();
            }
        }
    }
}