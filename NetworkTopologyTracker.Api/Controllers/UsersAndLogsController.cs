using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetworkTopologyTracker.Api.Data;
using System.Threading.Tasks;

namespace NetworkTopologyTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Sisteme giriş yapmamış (token'ı olmayan) kimse bu verilere erişemez!
    public class UsersAndLogsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersAndLogsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. KULLANICI LİSTELEME: Arkadaşının kullanıcı tablosunu (Ad, Soyad, Rol, Son Giriş) doldurur
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            // Şifreleri güvenlik sebebiyle arayüze göndermiyoruz, sadece gerekli alanları seçiyoruz
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Username,
                    u.Role,
                    u.LastLoginAt
                })
                .ToListAsync();

            return Ok(users);
        }

        // 2. LOG LİSTELEME: Gerçek zamanlı sistem loglarını en yeni tarihten en eskiye doğru listeler
        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs()
        {
            var logs = await _context.SystemLogs
                .OrderByDescending(l => l.Timestamp) // En son yapılan işlem en üstte gözüksün
                .ToListAsync();

            return Ok(logs);
        }
    }
}