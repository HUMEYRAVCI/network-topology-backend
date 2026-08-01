using Microsoft.EntityFrameworkCore;
using NetworkTopologyTracker.Api.Entities;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace NetworkTopologyTracker.Api.Data
{
    // ÖNEMLİ TASARIM KARARI: Bu sınıf kullanıcı (User) seed ediyor.
    // Facility/Floor/Room/Device/Port/Connection/Credential seed işlemi DbInitializer.cs'e devredildi İki ayrı seeder'ın aynı tabloları
    // (Facilities, Devices, Ports...) doldurmaya çalışması veri çakışmasına/
    // tekrarına yol açardı, bu yüzden sorumluluklar netçe ayrıldı.
    //   SeedData.cs -> SADECE Users

    public static class SeedData
    {
        public static async Task Initialize(AppDbContext context)
        {
            // Şifreler PasswordHasher<User> ile RUNTIME'da hash'leniyor.
            
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