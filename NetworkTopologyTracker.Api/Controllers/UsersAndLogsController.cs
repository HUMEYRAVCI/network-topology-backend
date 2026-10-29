using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetworkTopologyTracker.Api.Data;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkTopologyTracker.Api.Controllers
{
    /// <summary>
    /// Kullanıcı Yönetimi ve Denetim Logları Denetleyicisi (Users &amp; Audit Logs Controller).
    /// Sistemdeki kayıtlı kullanıcıların listelenmesi ve gerçekleştirilen işlemlere ait
    /// denetim kayıtlarının (SystemLogs) incelenmesi için yönetim uç noktalarını sunar.
    /// GÜVENLİK: Sınıf genelinde <see cref="AuthorizeAttribute"/> zorunludur ve içerideki tüm uç noktalar
    /// yalnızca "SuperAdmin" rolüne sahip yetkili yöneticiler tarafından çağrılabilir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Sisteme giriş yapmamış (JWT Token'ı olmayan) hiçbir istemci bu denetleyiciye erişemez.
    public class UsersAndLogsController : ControllerBase
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Veritabanı bağlamını (<see cref="AppDbContext"/>) Dependency Injection ile alır.
        /// </summary>
        public UsersAndLogsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Sistemde kayıtlı tüm kullanıcıları (şifre hash'leri hariç güvenli projeksiyon ile) listeler.
        /// Rota: GET /api/UsersAndLogs/users
        /// Yetki: Yalnızca "SuperAdmin"
        /// </summary>
        [HttpGet("users")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetUsers()
        {
            // AsNoTracking() ile salt-okunur sorgu performansı optimize edilir.
            // Select() projeksiyonu kullanılarak Password (hash) alanı yanıttan tamamen hariç tutulur.
            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Username,
                    u.Role,
                    // Kullanıcının son giriş tarihi varsa okunabilir formatta, yoksa bilgilendirici metin olarak döner.
                    LastLoginAt = u.LastLoginAt.HasValue 
                        ? u.LastLoginAt.Value.ToString("yyyy-MM-dd HH:mm:ss") 
                        : "Hiç Giriş Yapmadı"
                })
                .ToListAsync();

            return Ok(users);
        }

        /// <summary>
        /// Sistem denetim loglarını (Audit Trail) en yeniden en eskiye doğru sıralayarak getirir.
        /// Rota: GET /api/UsersAndLogs/logs?limit=100
        /// Yetki: Yalnızca "SuperAdmin"
        /// </summary>
        /// <param name="limit">Getirilecek maksimum log sayısı (Varsayılan: 100).</param>
        [HttpGet("logs")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetLogs([FromQuery] int limit = 100)
        {
            // Loglar zaman damgasına (Timestamp) göre azalan sırada (en güncel işlem en üstte) çekilir.
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