using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetworkTopologyTracker.Api.Data;
using NetworkTopologyTracker.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace NetworkTopologyTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TopologyController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TopologyController(AppDbContext context)
        {
            _context = context;
        }

        // Cihaz Ekleme / Güncelleme İstek Modeli (Brand tamamen kaldırıldı)
        public class DeviceRequest
        {
            public string Name { get; set; } = string.Empty;
            public string DeviceType { get; set; } = string.Empty;
            public string Status { get; set; } = "Active";
            public string? Model { get; set; } // Örn: "Cisco Catalyst 2960"
            public string? MacAddress { get; set; }
            public string? SerialNumber { get; set; }
            public int RoomId { get; set; }
        }

        // 1. UÇ: Tüm Tesisleri Listeler
        [HttpGet("facilities")]
        public async Task<IActionResult> GetFacilities()
        {
            var facilities = await _context.Facilities
                .AsNoTracking()
                .Select(f => new { f.Id, f.Name, f.Address })
                .ToListAsync();

            return Ok(facilities);
        }

        // 2. UÇ: Seçilen Tesisteki Odaları, Katları ve Cihazları Getirir
        [HttpGet("facilities/{facilityId}/topology")]
        public async Task<IActionResult> GetTopology(int facilityId)
        {
            var rooms = await _context.Rooms
                .AsNoTracking()
                .Include(r => r.Floor)
                .Where(r => r.Floor != null && r.Floor.FacilityId == facilityId)
                .Include(r => r.Devices)
                    .ThenInclude(d => d.Ports)
                .ToListAsync();

            var connections = await _context.Connections
                .AsNoTracking()
                .Include(c => c.SourcePort)
                    .ThenInclude(p => p!.Device)
                        .ThenInclude(d => d!.Room)
                            .ThenInclude(r => r!.Floor)
                .Where(c => c.SourcePort != null && 
                            c.SourcePort.Device != null && 
                            c.SourcePort.Device.Room != null && 
                            c.SourcePort.Device.Room.Floor != null && 
                            c.SourcePort.Device.Room.Floor.FacilityId == facilityId)
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
                    FloorName = r.Floor != null ? r.Floor.Name : "Bilinmeyen Kat",
                    Devices = r.Devices.Select(d => new
                    {
                        d.Id,
                        d.Name,
                        d.DeviceType,
                        d.Status,
                        d.Model // Doğrudan tek model alanı döner
                    })
                }),
                Connections = connections
            };

            return Ok(result);
        }

        // 3. UÇ: Cihaz Detayı
        [HttpGet("devices/{id}")]
        public async Task<IActionResult> GetDeviceDetails(int id)
        {
            var device = await _context.Devices
                .AsNoTracking()
                .Include(d => d.Ports)
                .Include(d => d.Credential)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (device == null) return NotFound(new { message = "Cihaz bulunamadı." });

            bool isSuperAdmin = User.IsInRole("SuperAdmin");

            string ipAddress = "GİZLİ (Yetki Yok)";
            string username = "GİZLİ (Yetki Yok)";
            string password = "GİZLİ (Yetki Yok)";

            if (isSuperAdmin && device.Credential != null)
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
                device.Model, // Doğrudan tek model alanı
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

        // 4. UÇ: Yeni Cihaz Ekleme (POST)
        [HttpPost("devices")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CreateDevice([FromBody] DeviceRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Cihaz bilgileri boş olamaz." });
            }

            var room = await _context.Rooms.FindAsync(request.RoomId);
            if (room == null)
            {
                return BadRequest(new { message = "Seçilen oda veritabanında bulunamadı." });
            }

            var newDevice = new Device
            {
                Name = request.Name,
                DeviceType = request.DeviceType,
                Status = request.Status,
                Model = request.Model,
                MacAddress = request.MacAddress,
                SerialNumber = request.SerialNumber,
                RoomId = request.RoomId
            };

            await _context.Devices.AddAsync(newDevice);

            var currentUsername = User.Identity?.Name ?? "Sistem";
            var log = new SystemLog
            {
                Username = currentUsername,
                Action = "Cihaz Eklendi",
                Details = $"'{request.Name}' isimli yeni cihaz eklendi.",
                ActionType = "Ekleme",
                Timestamp = DateTime.UtcNow
            };
            await _context.SystemLogs.AddAsync(log);

            await _context.SaveChangesAsync();

            return Ok(newDevice);
        }

        // 5. UÇ: Cihaz Silme (DELETE)
        [HttpDelete("devices/{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeleteDevice(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            
            if (device == null)
            {
                return NotFound(new { message = $"ID'si {id} olan cihaz bulunamadı." });
            }

            _context.Devices.Remove(device);

            var currentUsername = User.Identity?.Name ?? "Sistem";
            var log = new SystemLog
            {
                Username = currentUsername,
                Action = "Cihaz Silindi",
                Details = $"'{device.Name}' (ID: {id}) cihazı sistemden silindi.",
                ActionType = "Silme",
                Timestamp = DateTime.UtcNow
            };
            await _context.SystemLogs.AddAsync(log);

            await _context.SaveChangesAsync();

            return Ok(new { message = $"'{device.Name}' başarıyla silindi." });
        }

        // 6. UÇ: Cihaz Bilgilerini Güncelleme (PUT)
        [HttpPut("devices/{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateDevice(int id, [FromBody] DeviceRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Güncelleme bilgileri boş olamaz." });
            }

            var existingDevice = await _context.Devices.FirstOrDefaultAsync(d => d.Id == id);
            if (existingDevice == null)
            {
                return NotFound(new { message = $"Güncellenmek istenen {id} ID'li cihaz bulunamadı." });
            }

            var currentUsername = User.Identity?.Name ?? "Sistem";

            existingDevice.Name = request.Name;
            existingDevice.DeviceType = request.DeviceType;
            existingDevice.Status = request.Status;
            existingDevice.Model = request.Model;
            existingDevice.MacAddress = request.MacAddress;
            existingDevice.SerialNumber = request.SerialNumber;
            existingDevice.RoomId = request.RoomId;

            var log = new SystemLog
            {
                Username = currentUsername,
                Action = "Cihaz Güncellendi",
                Details = $"{id} ID'li cihazın bilgileri güncellendi.",
                ActionType = "Güncelleme",
                Timestamp = DateTime.UtcNow
            };
            await _context.SystemLogs.AddAsync(log);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cihaz başarıyla güncellendi!", device = existingDevice });
        }

        // 7. UÇ: Cihaz Arama (GET)
        [HttpGet("devices/search")]
        public async Task<IActionResult> SearchDevices([FromQuery] string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Ok(await _context.Devices.AsNoTracking().ToListAsync());

            var results = await _context.Devices
                .AsNoTracking()
                .Where(d => d.Name.Contains(query) || (d.MacAddress != null && d.MacAddress.Contains(query)))
                .ToListAsync();

            return Ok(results);
        }

        // 8. UÇ: Son 50 Sistem Logunu Getirir (GET)
        [HttpGet("logs")]
        public async Task<IActionResult> GetSystemLogs()
        {
            var logs = await _context.SystemLogs
                .AsNoTracking()
                .OrderByDescending(l => l.Timestamp)
                .Take(50)
                .ToListAsync();

            return Ok(logs);
        }
    }
}