using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetworkTopologyTracker.Api.Entities;
using System;
using System.Threading.Tasks;

namespace NetworkTopologyTracker.Api.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(AppDbContext context)
        {
            // 1. Şifreleme aracımızı C# tarafında hazırlıyoruz
            var hasher = new PasswordHasher<User>();

            // 2. SUPERADMIN (humeyra) KULLANICISI KONTROLÜ
            var superAdmin = await context.Users.FirstOrDefaultAsync(u => u.Username == "humeyra");
            if (superAdmin == null)
            {
                superAdmin = new User
                {
                    Username = "humeyra",
                    FirstName = "Hümeyra",
                    LastName = "Avcı",
                    Role = "SuperAdmin"
                };
                superAdmin.Password = hasher.HashPassword(superAdmin, "Password123!");
                await context.Users.AddAsync(superAdmin);
            }
            else if (string.IsNullOrEmpty(superAdmin.Password))
            {
                // DBeaver tarafında temizlediğimiz şifreyi C# ile %100 doğru hash'liyoruz
                superAdmin.Password = hasher.HashPassword(superAdmin, "Password123!");
            }

            // 3. VIEWER (ahmet_viewer) KULLANICISI KONTROLÜ
            var viewer = await context.Users.FirstOrDefaultAsync(u => u.Username == "ahmet_viewer");
            if (viewer == null)
            {
                viewer = new User
                {
                    Username = "ahmet_viewer",
                    FirstName = "Ahmet",
                    LastName = "İzleyici",
                    Role = "Viewer"
                };
                viewer.Password = hasher.HashPassword(viewer, "viewer123!");
                await context.Users.AddAsync(viewer);
            }
            else if (string.IsNullOrEmpty(viewer.Password))
            {
                viewer.Password = hasher.HashPassword(viewer, "viewer123!");
            }

            // 4. Değişiklikleri veritabanına kaydet
            await context.SaveChangesAsync();
        }
    }
}