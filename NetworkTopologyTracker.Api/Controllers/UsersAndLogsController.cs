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
            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Username,
                    u.Role,
                    // Türkiye Saat Dilimine Göre Temiz ve Okunabilir Format
                    LastLoginAt = u.LastLoginAt.HasValue 
                        ? u.LastLoginAt.Value.ToString("yyyy-MM-dd HH:mm:ss") 
                        : "Hiç Giriş Yapmadı"
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
                .AsNoTracking()
                .OrderByDescending(l => l.Timestamp)
                .Take(limit)
                .Select(l => new
                {
                    l.Id,
                    l.Username,
                    l.Action,
                    l.Details,
                    l.ActionType,
                    Timestamp = l.Timestamp.ToString("yyyy-MM-dd HH:mm:ss") // Türkiye saatli temiz log zamanı
                })
                .ToListAsync();

            return Ok(logs);
        }
    }
}