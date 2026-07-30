using Microsoft.EntityFrameworkCore;
using NetworkTopologyTracker.Api.Entities;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace NetworkTopologyTracker.Api.Data
{
    // ÖNEMLİ TASARIM KARARI: Bu sınıf ARTIK SADECE kullanıcı (User) seed ediyor.
    // Facility/Floor/Room/Device/Port/Connection/Credential seed işlemi TAMAMEN
    // DbInitializer.cs'e devredildi — çünkü DbInitializer.cs, cihaz başına gerçekçi
    // port sayıları (Router->24, Switch->parametrik, AP->2 vb.), gerçek Credential'lar
    // ve gerçek Connection bağlantıları içeren çok daha kapsamlı, tek doğruluk
    // kaynağı bir seed mantığı sunuyor. İki ayrı seeder'ın aynı tabloları
    // (Facilities, Devices, Ports...) doldurmaya çalışması veri çakışmasına/
    // tekrarına yol açardı, bu yüzden sorumluluklar netçe ayrıldı:
    //   SeedData.cs       -> SADECE Users
    //   DbInitializer.cs  -> Facility/Floor/Room/Device/Port/Connection/Credential
    public static class SeedData
    {
        public static async Task Initialize(AppDbContext context)
        {
            // Şifreler PasswordHasher<User> ile RUNTIME'da hash'leniyor.
            // Bu, doğru ve sürdürülebilir tek yöntemdir; asla HasData içine
            // elle yazılmış bir hash string'i konulmamalıdır (salt her çalıştırmada
            // farklı üretilir, statik bir string asla doğrulanamaz).
            if (!await context.Users.AnyAsync())
            {
                var hasher = new PasswordHasher<User>();

                var superAdmin = new User
                {
                    FirstName = "Hümeyra",
                    LastName = "Avcı",
                    Username = "humeyra",
                    Role = "SuperAdmin"
                };
                superAdmin.Password = hasher.HashPassword(superAdmin, "Password123!");

                var viewer = new User
                {
                    FirstName = "Ahmet",
                    LastName = "Yılmaz",
                    Username = "ahmet_viewer",
                    Role = "Viewer"
                };
                viewer.Password = hasher.HashPassword(viewer, "Viewer123!");

                await context.Users.AddRangeAsync(superAdmin, viewer);
                await context.SaveChangesAsync();
            }
        }
    }
}