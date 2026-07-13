using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetworkTopologyTracker.Api.Data;
using NetworkTopologyTracker.Api.Entities;

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
                .Select(f => new { f.Id, f.Name, f.Address })
                .ToListAsync();

            return Ok(facilities);
        }

        // 2. UÇ: Seçilen Tesisteki Odaları ve Cihazları Getirir (Topoloji Çizimi İçin)
        [HttpGet("facilities/{facilityId}/topology")]
        public async Task<IActionResult> GetTopology(int facilityId)
        {
            // Tesise ait odaları, o odalardaki cihazları ve o cihazların bağlı olduğu portları getiriyoruz
            var rooms = await _context.Rooms
                .Where(r => r.FacilityId == facilityId)
                .Include(r => r.Devices)
                    .ThenInclude(d => d.Ports)
                .ToListAsync();

            // Tüm ağın bağlantı haritasını (çizgilerini) çekiyoruz
            var connections = await _context.Connections
                .Include(c => c.SourcePort)
                .Where(c => c.SourcePort!.Device!.Room!.FacilityId == facilityId)
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
                    r.FloorInfo,
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

        // 3. UÇ: Yan Paneli Besleyen Cihaz Detayı (GİZLİLİK VE YETKİ KONTROLLÜ)
        // Kullanıcı "isAdmin=true" parametresi göndermezse şifre alanları boş gider!
        [HttpGet("devices/{id}")]
        public async Task<IActionResult> GetDeviceDetails(int id, [FromQuery] bool isAdmin = false)
        {
            var device = await _context.Devices
                .Include(d => d.Ports)
                .Include(d => d.Credential)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (device == null) return NotFound("Cihaz bulunamadı.");

            // Güvenlik filtreleme simülasyonu
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
                // Hassas veriler yetkiye göre dolduruluyor
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
    }
}