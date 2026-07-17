using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetworkTopologyTracker.Api.Data;
using NetworkTopologyTracker.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;

namespace NetworkTopologyTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TopologyController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TopologyController(AppDbContext context)
        {
            _context = context;
        }

        // 1. UÇ: Tüm Tesisleri Listeler (Arayüzdeki Tesis Seçim Dropdown'ı için)
        [HttpGet("facilities")]
        public async Task<IActionResult> GetFacilities()
        {
            var facilities = await _context.Facilities
                .Select(f => new { f.Id, f.Name, f.Address, f.InstitutionId })
                .ToListAsync();

            return Ok(facilities);
        }

        // 2. UÇ: Seçilen Tesisteki Odaları, Katları ve Cihazları Getirir (YENİ HİYERARŞİYE UYUMLU)
        [HttpGet("facilities/{facilityId}/topology")]
        public async Task<IActionResult> GetTopology(int facilityId)
        {
            // Odaları, bağlı oldukları Kat (Floor) bilgisi, odadaki cihazlar ve cihazların portlarıyla çekiyoruz.
            // Eski 'FacilityId' odada olmadığı için kat üzerinden binaya gidiyoruz: Floor.FacilityId
            var rooms = await _context.Rooms
                .Include(r => r.Floor)
                .Where(r => r.Floor!.FacilityId == facilityId)
                .Include(r => r.Devices)
                    .ThenInclude(d => d.Ports)
                .ToListAsync();

            // Tüm ağın bağlantı haritasını (kablo çizgilerini) yeni kat ilişkisine göre çekiyoruz
            var connections = await _context.Connections
                .Include(c => c.SourcePort)
                .Where(c => c.SourcePort!.Device!.Room!.Floor!.FacilityId == facilityId)
                .Select(c => new
                {
                    c.Id,
                    c.ConnectionType,
                    SourceDeviceId = c.SourcePort!.DeviceId,
                    SourcePortName = c.SourcePort.PortNumber,
                    TargetDeviceId = c.TargetDeviceId
                })
                .ToListAsync();

            var result = new
            {
                Rooms = rooms.Select(r => new
                {
                    r.Id,
                    r.Name,
                    FloorId = r.FloorId,
                    FloorName = r.Floor != null ? r.Floor.Name : "Bilinmeyen Kat", // Eski FloorInfo yerine kat ismi gidiyor
                    Devices = r.Devices.Select(d => new
                    {
                        d.Id,
                        d.Name,
                        d.DeviceType,
                        d.Status,
                        d.Brand,
                        d.Model
                    })
                }),
                Connections = connections
            };

            return Ok(result);
        }

        // 3. UÇ: Yan Paneli Besleyen Cihaz Detayı (GİZLİLİK VE YETKİ KONTROLLÜ - AYNEN KORUNDU)
        [HttpGet("devices/{id}")]
        public async Task<IActionResult> GetDeviceDetails(int id, [FromQuery] bool isAdmin = false)
        {
            var device = await _context.Devices
                .Include(d => d.Ports)
                .Include(d => d.Credential)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (device == null) return NotFound("Cihaz bulunamadı.");

            string ipAddress = "GİZLİ (Yetki Yok)";
            string username = "GİZLİ (Yetki Yok)";
            string password = "GİZLİ (Yetki Yok)";

            if (isAdmin && device.Credential != null)
            {
                ipAddress = device.Credential.IpAddress;
                username = device.Credential.Username;
                password = device.Credential.Password;
            }

            var response = new
            {
                device.Id,
                device.Name,
                device.DeviceType,
                device.Status,
                device.Brand,
                device.Model,
                device.MacAddress,
                device.SerialNumber,
                SecurityInfo = new
                {
                    IpAddress = ipAddress,
                    Username = username,
                    Password = password
                },
                Ports = device.Ports.Select(p => new { p.Id, p.PortNumber, p.Status })
            };

            return Ok(response);
        }

        // 4. UÇ: Yeni Cihaz Ekleme (POST - AYNEN KORUNDU)
        [HttpPost("devices")]
        public async Task<IActionResult> CreateDevice([FromBody] Device newDevice)
        {
            if (newDevice == null)
            {
                return BadRequest("Cihaz bilgileri boş olamaz.");
            }

            _context.Devices.Add(newDevice);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDeviceDetails), new { id = newDevice.Id }, newDevice);
        }

        // 5. UÇ: Cihaz Silme (DELETE - AYNEN KORUNDU)
        [HttpDelete("devices/{id}")]
        public async Task<IActionResult> DeleteDevice(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            
            if (device == null)
            {
                return NotFound($"ID'si {id} olan cihaz bulunamadı.");
            }

            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}