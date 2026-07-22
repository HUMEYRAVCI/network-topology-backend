using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetworkTopologyTracker.Api.Data;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkTopologyTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Sisteme giriş yapmamış kimse bu uçlara erişemez
    public class UsersAndLogsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersAndLogsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. KULLANICI LİSTELEME: Sadece SuperAdmin kullanıcı listesini görebilir
        [HttpGet("users")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetUsers()
        {
            // Şifreleri güvenlik sebebiyle arayüze göndermiyoruz
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

        // 2. LOG LİSTELEME: Sadece SuperAdmin sistem loglarını görebilir
        [HttpGet("logs")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetLogs([FromQuery] int limit = 100)
        {
            var logs = await _context.SystemLogs
                .OrderByDescending(l => l.Timestamp) // En son yapılan işlem en üstte
                .Take(limit) // Performans için varsayılan son 100 log getirilir
                .ToListAsync();

            return Ok(logs);
        }
    }
}