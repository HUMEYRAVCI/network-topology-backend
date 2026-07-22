using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetworkTopologyTracker.Api.Data;
using NetworkTopologyTracker.Api.Entities;
using System;
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
            public string? Description { get; set; }
        }

        public InstitutionsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. LİSTELEME: Tüm kurumları ve altındaki tesisleri getirir
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var institutions = await _context.Institutions
                .Include(i => i.Facilities)
                .ToListAsync();

            return Ok(institutions);
        }

        // 1.1 TEK BİR KURUMU GETİRME
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var institution = await _context.Institutions
                .Include(i => i.Facilities)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (institution == null)
            {
                return NotFound(new { message = "Kurum bulunamadı!" });
            }

            return Ok(institution);
        }

        // 2. EKLEME: Sadece SuperAdmin yeni bir kurum/bina ekleyebilir
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Create([FromBody] InstitutionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Kurum adı boş olamaz!" });
            }

            var institution = new Institution
            {
                Name = request.Name,
                Description = request.Description
            };

            await _context.Institutions.AddAsync(institution);

            // 🪵 GERÇEK ZAMANLI LOG
            var username = User.Identity?.Name ?? "Bilinmeyen Kullanıcı";
            var log = new SystemLog
            {
                Username = username,
                Action = "Kurum Eklendi",
                Details = $"'{request.Name}' isimli yeni kurum sisteme eklendi.",
                ActionType = "Ekleme",
                Timestamp = DateTime.UtcNow
            };
            await _context.SystemLogs.AddAsync(log);

            await _context.SaveChangesAsync();
            return Ok(institution);
        }

        // 3. DÜZENLEME: Sadece SuperAdmin kurum bilgilerini güncelleyebilir
        [HttpPut("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Update(int id, [FromBody] InstitutionRequest request)
        {
            var institution = await _context.Institutions.FindAsync(id);
            if (institution == null)
            {
                return NotFound(new { message = "Güncellenecek kurum bulunamadı!" });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Kurum adı boş olamaz!" });
            }

            var oldName = institution.Name;
            institution.Name = request.Name;
            institution.Description = request.Description;

            // 🪵 GERÇEK ZAMANLI LOG
            var username = User.Identity?.Name ?? "Bilinmeyen Kullanıcı";
            var log = new SystemLog
            {
                Username = username,
                Action = "Kurum Düzenlendi",
                Details = $"Kurum ismi '{oldName}' değerinden '{request.Name}' değerine güncellendi.",
                ActionType = "Güncelleme",
                Timestamp = DateTime.UtcNow
            };
            await _context.SystemLogs.AddAsync(log);

            await _context.SaveChangesAsync();
            return Ok(institution);
        }

        // 4. SİLME: Sadece SuperAdmin kurumu ve bağlı tüm hiyerarşiyi silebilir
        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var institution = await _context.Institutions.FindAsync(id);
            if (institution == null)
            {
                return NotFound(new { message = "Silinecek kurum bulunamadı!" });
            }

            _context.Institutions.Remove(institution);

            // 🪵 GERÇEK ZAMANLI LOG
            var username = User.Identity?.Name ?? "Bilinmeyen Kullanıcı";
            var log = new SystemLog
            {
                Username = username,
                Action = "Kurum Silindi",
                Details = $"'{institution.Name}' isimli kurum ve bağlı tüm alt katmanları sistemden silindi.",
                ActionType = "Silme",
                Timestamp = DateTime.UtcNow
            };
            await _context.SystemLogs.AddAsync(log);

            await _context.SaveChangesAsync();
            return Ok(new { message = $"'{institution.Name}' başarıyla sistemden kaldırıldı." });
        }
    }
}