using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetworkTopologyTracker.Api.Data;
using NetworkTopologyTracker.Api.Entities;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NetworkTopologyTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Sisteme giriş yapmamış hiç kimse bu uçlara erişemez!
    public class InstitutionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public class InstitutionRequest
        {
            public string Name { get; set; } = string.Empty;
        }

        public InstitutionsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. LİSTELEME: Tüm binaları/kurumları getirir (Herkes görebilir)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var institutions = await _context.Institutions.ToListAsync();
            return Ok(institutions);
        }

        // 2. EKLEME: Sadece SuperAdmin yeni bir bina ekleyebilir
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Create([FromBody] InstitutionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Bina adı boş olamaz!" });
            }

            var institution = new Institution
            {
                Name = request.Name
            };

            await _context.Institutions.AddAsync(institution);

            // 🪵 GERÇEK ZAMANLI LOG: Kimin ne eklediğini sisteme yazıyoruz
            var username = User.Identity?.Name ?? "Bilinmeyen Kullanıcı";
            var log = new SystemLog
            {
                Username = username,
                Action = "Bina Eklendi",
                Details = $"'{request.Name}' isimli yeni bir bina/kurum sisteme eklendi.",
                ActionType = "Ekleme",
                Timestamp = DateTime.UtcNow
            };
            await _context.SystemLogs.AddAsync(log);

            await _context.SaveChangesAsync();
            return Ok(institution);
        }

        // 3. DÜZENLEME: Sadece SuperAdmin binanın bilgilerini güncelleyebilir
        [HttpPut("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Update(int id, [FromBody] InstitutionRequest request)
        {
            var institution = await _context.Institutions.FindAsync(id);
            if (institution == null)
            {
                return NotFound(new { message = "Güncellenecek bina bulunamadı!" });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Bina adı boş olamaz!" });
            }

            var oldName = institution.Name;
            institution.Name = request.Name;

            // 🪵 GERÇEK ZAMANLI LOG: Düzenleme geçmişini kaydet
            var username = User.Identity?.Name ?? "Bilinmeyen Kullanıcı";
            var log = new SystemLog
            {
                Username = username,
                Action = "Bina Düzenlendi",
                Details = $"Binanın ismi '{oldName}' değerinden '{request.Name}' değerine güncellendi.",
                ActionType = "Güncelleme",
                Timestamp = DateTime.UtcNow
            };
            await _context.SystemLogs.AddAsync(log);

            await _context.SaveChangesAsync();
            return Ok(institution);
        }

        // 4. SİLME: Sadece SuperAdmin binayı ve ona bağlı alt hiyerarşiyi silebilir
        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var institution = await _context.Institutions.FindAsync(id);
            if (institution == null)
            {
                return NotFound(new { message = "Silinecek bina bulunamadı!" });
            }

            _context.Institutions.Remove(institution);

            // 🪵 GERÇEK ZAMANLI LOG: Kim neyi sildi bilmeliyiz
            var username = User.Identity?.Name ?? "Bilinmeyen Kullanıcı";
            var log = new SystemLog
            {
                Username = username,
                Action = "Bina Silindi",
                Details = $"'{institution.Name}' isimli bina ve bağlı tüm alt katmanları sistemden silindi.",
                ActionType = "Silme",
                Timestamp = DateTime.UtcNow
            };
            await _context.SystemLogs.AddAsync(log);

            await _context.SaveChangesAsync();
            return Ok(new { message = $"'{institution.Name}' başarıyla sistemden kaldırıldı." });
        }
    }
}