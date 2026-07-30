using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetworkTopologyTracker.Api.Data;
using NetworkTopologyTracker.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.ComponentModel.DataAnnotations;
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

        // =========================================================================
        // 1. BİNA (FACILITY) İŞLEMLERİ
        // =========================================================================

        /// <summary>
        /// Tüm binaların listesini getirir.
        /// </summary>
        [HttpGet("facilities")]
        public async Task<IActionResult> GetFacilities()
        {
            var facilities = await _context.Facilities
                .AsNoTracking()
                .Select(f => new { f.Id, f.Name, f.FacilityType, f.Address })
                .ToListAsync();

            return Ok(facilities);
        }

        /// <summary>
        /// Yeni bir bina tanımlar.
        /// </summary>
        [HttpPost("facilities")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CreateFacility([FromBody] FacilityRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Bina adı boş olamaz." });

            var facility = new Facility
            {
                Name = request.Name,
                FacilityType = string.IsNullOrWhiteSpace(request.FacilityType) ? "Hizmet Binası" : request.FacilityType,
                Address = request.Address,
                CreatedAt = DateTime.UtcNow.AddHours(3)
            };

            await _context.Facilities.AddAsync(facility);

            var currentUsername = User.Identity?.Name ?? "Sistem";
            await _context.SystemLogs.AddAsync(new SystemLog
            {
                Username = currentUsername,
                Action = "Bina Eklendi",
                Details = $"'{request.Name}' isimli yeni tesis eklendi.",
                ActionType = "Ekleme",
                Timestamp = DateTime.UtcNow.AddHours(3)
            });

            await _context.SaveChangesAsync();

            return Ok(facility);
        }

        /// <summary>
        /// Seçilen tesisteki odaları, katları, cihazları ve bağlantı ağacını getirir.
        /// </summary>
        [HttpGet("facilities/{facilityId}/topology")]
        public async Task<IActionResult> GetTopology(int facilityId)
        {
            var rooms = await _context.Rooms
                .AsNoTracking()
                .Include(r => r.Floor)
                .Where(r => r.Floor != null && r.Floor.FacilityId == facilityId)
                .Include(r => r.Devices)
                    .ThenInclude(d => d.Ports)
                .Include(r => r.Devices)
                    .ThenInclude(d => d.Credential)
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
                        d.Model,
                        d.MacAddress,
                        d.SerialNumber,
                        SecurityInfo = BuildSecurityInfo(d.Credential),
                        PortCount = d.Ports.Count,
                        Ports = d.Ports.Select(p => new { p.Id, p.PortNumber, p.Status })
                    })
                }),
                Connections = connections
            };

            return Ok(result);
        }

        /// <summary>
        /// Var olan bir binanın bilgilerini günceller.
        /// </summary>
        [HttpPut("facilities/{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateFacility(int id, [FromBody] FacilityRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Bina adı boş olamaz." });

            var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == id);
            if (facility == null)
                return NotFound(new { message = $"ID'si {id} olan tesis bulunamadı." });

            facility.Name = request.Name;
            facility.FacilityType = string.IsNullOrWhiteSpace(request.FacilityType) ? facility.FacilityType : request.FacilityType;
            facility.Address = request.Address;

            var currentUsername = User.Identity?.Name ?? "Sistem";
            await _context.SystemLogs.AddAsync(new SystemLog
            {
                Username = currentUsername,
                Action = "Bina Güncellendi",
                Details = $"ID:{id} tesisi güncellendi.",
                ActionType = "Güncelleme",
                Timestamp = DateTime.UtcNow.AddHours(3)
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "Bina başarıyla güncellendi!", facility });
        }

        /// <summary>
        /// Binayı ve ona bağlı alt öğeleri siler (confirm=true parametresi gerektirir).
        /// </summary>
        [HttpDelete("facilities/{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeleteFacility(int id, [FromQuery] bool confirm = false)
        {
            var facility = await _context.Facilities
                .Include(f => f.Floors)
                    .ThenInclude(fl => fl.Rooms)
                        .ThenInclude(r => r.Devices)
                            .ThenInclude(d => d.Ports)
                .Include(f => f.Floors)
                    .ThenInclude(fl => fl.Rooms)
                        .ThenInclude(r => r.Devices)
                            .ThenInclude(d => d.Credential)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (facility == null)
                return NotFound(new { message = $"ID'si {id} olan tesis bulunamadı." });

            var floors = facility.Floors.ToList();
            var rooms = floors.SelectMany(fl => fl.Rooms).ToList();
            var devices = rooms.SelectMany(r => r.Devices).ToList();
            var deviceIds = devices.Select(d => d.Id).ToList();
            var ports = devices.SelectMany(d => d.Ports).ToList();
            var credentials = devices.Where(d => d.Credential != null).Select(d => d.Credential!).ToList();
            var connections = await FindRelatedConnectionsAsync(deviceIds);

            if (!confirm)
            {
                return Conflict(new
                {
                    message = $"'{facility.Name}' silinirse aşağıdaki kayıtlar da kalıcı olarak silinecek. Onaylamak için isteği ?confirm=true ile tekrar gönderin.",
                    willDelete = new
                    {
                        Floors = floors.Count,
                        Rooms = rooms.Count,
                        Devices = devices.Count,
                        Ports = ports.Count,
                        Credentials = credentials.Count,
                        Connections = connections.Count
                    }
                });
            }

            _context.Connections.RemoveRange(connections);
            _context.Facilities.Remove(facility);

            var currentUsername = User.Identity?.Name ?? "Sistem";
            await _context.SystemLogs.AddAsync(new SystemLog
            {
                Username = currentUsername,
                Action = "Bina Silindi",
                Details = $"'{facility.Name}' (ID:{id}) silindi. Birlikte silinenler: {floors.Count} kat, {rooms.Count} oda, {devices.Count} cihaz, {ports.Count} port, {credentials.Count} credential, {connections.Count} bağlantı.",
                ActionType = "Silme",
                Timestamp = DateTime.UtcNow.AddHours(3)
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = $"'{facility.Name}' ve bağlı tüm kayıtlar başarıyla silindi." });
        }

        // =========================================================================
        // 2. KAT (FLOOR) İŞLEMLERİ
        // =========================================================================

        /// <summary>
        /// Seçilen binaya yeni bir kat ekler.
        /// </summary>
        [HttpPost("floors")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CreateFloor([FromBody] FloorRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Kat adı boş olamaz." });

            var facilityExists = await _context.Facilities.AnyAsync(f => f.Id == request.FacilityId);
            if (!facilityExists)
                return NotFound(new { message = "Seçilen bina bulunamadı." });

            var floor = new Floor
            {
                Name = request.Name,
                Level = request.Level,
                FacilityId = request.FacilityId
            };

            await _context.Floors.AddAsync(floor);

            var currentUsername = User.Identity?.Name ?? "Sistem";
            await _context.SystemLogs.AddAsync(new SystemLog
            {
                Username = currentUsername,
                Action = "Kat Eklendi",
                Details = $"ID:{request.FacilityId} tesisine '{request.Name}' eklendi.",
                ActionType = "Ekleme",
                Timestamp = DateTime.UtcNow.AddHours(3)
            });

            await _context.SaveChangesAsync();

            return Ok(floor);
        }

        /// <summary>
        /// Var olan katın bilgilerini günceller.
        /// </summary>
        [HttpPut("floors/{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateFloor(int id, [FromBody] FloorRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Kat adı boş olamaz." });

            var floor = await _context.Floors.FirstOrDefaultAsync(f => f.Id == id);
            if (floor == null)
                return NotFound(new { message = $"ID'si {id} olan kat bulunamadı." });

            var facilityExists = await _context.Facilities.AnyAsync(f => f.Id == request.FacilityId);
            if (!facilityExists)
                return NotFound(new { message = "Seçilen bina bulunamadı." });

            floor.Name = request.Name;
            floor.Level = request.Level;
            floor.FacilityId = request.FacilityId;

            var currentUsername = User.Identity?.Name ?? "Sistem";
            await _context.SystemLogs.AddAsync(new SystemLog
            {
                Username = currentUsername,
                Action = "Kat Güncellendi",
                Details = $"ID:{id} katı güncellendi.",
                ActionType = "Güncelleme",
                Timestamp = DateTime.UtcNow.AddHours(3)
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "Kat başarıyla güncellendi!", floor });
        }

        /// <summary>
        /// Katı ve altındaki odaları/cihazları siler (confirm=true parametresi gerektirir).
        /// </summary>
        [HttpDelete("floors/{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeleteFloor(int id, [FromQuery] bool confirm = false)
        {
            var floor = await _context.Floors
                .Include(fl => fl.Rooms)
                    .ThenInclude(r => r.Devices)
                        .ThenInclude(d => d.Ports)
                .Include(fl => fl.Rooms)
                    .ThenInclude(r => r.Devices)
                        .ThenInclude(d => d.Credential)
                .FirstOrDefaultAsync(fl => fl.Id == id);

            if (floor == null)
                return NotFound(new { message = $"ID'si {id} olan kat bulunamadı." });

            var rooms = floor.Rooms.ToList();
            var devices = rooms.SelectMany(r => r.Devices).ToList();
            var deviceIds = devices.Select(d => d.Id).ToList();
            var ports = devices.SelectMany(d => d.Ports).ToList();
            var credentials = devices.Where(d => d.Credential != null).Select(d => d.Credential!).ToList();
            var connections = await FindRelatedConnectionsAsync(deviceIds);

            if (!confirm)
            {
                return Conflict(new
                {
                    message = $"'{floor.Name}' silinirse aşağıdaki kayıtlar da kalıcı olarak silinecek. Onaylamak için isteği ?confirm=true ile tekrar gönderin.",
                    willDelete = new
                    {
                        Rooms = rooms.Count,
                        Devices = devices.Count,
                        Ports = ports.Count,
                        Credentials = credentials.Count,
                        Connections = connections.Count
                    }
                });
            }

            _context.Connections.RemoveRange(connections);
            _context.Floors.Remove(floor);

            var currentUsername = User.Identity?.Name ?? "Sistem";
            await _context.SystemLogs.AddAsync(new SystemLog
            {
                Username = currentUsername,
                Action = "Kat Silindi",
                Details = $"'{floor.Name}' (ID:{id}) silindi. Birlikte silinenler: {rooms.Count} oda, {devices.Count} cihaz, {ports.Count} port, {credentials.Count} credential, {connections.Count} bağlantı.",
                ActionType = "Silme",
                Timestamp = DateTime.UtcNow.AddHours(3)
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = $"'{floor.Name}' ve bağlı tüm kayıtlar başarıyla silindi." });
        }

        // =========================================================================
        // 3. ODA (ROOM) İŞLEMLERİ
        // =========================================================================

        /// <summary>
        /// Seçilen kata yeni bir oda ekler.
        /// </summary>
        [HttpPost("rooms")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CreateRoom([FromBody] RoomRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Oda adı boş olamaz." });

            var floorExists = await _context.Floors.AnyAsync(f => f.Id == request.FloorId);
            if (!floorExists)
                return NotFound(new { message = "Seçilen kat bulunamadı." });

            var room = new Room
            {
                Name = request.Name,
                FloorId = request.FloorId,
                CreatedAt = DateTime.UtcNow.AddHours(3)
            };

            await _context.Rooms.AddAsync(room);

            var currentUsername = User.Identity?.Name ?? "Sistem";
            await _context.SystemLogs.AddAsync(new SystemLog
            {
                Username = currentUsername,
                Action = "Oda Eklendi",
                Details = $"ID:{request.FloorId} katına '{request.Name}' eklendi.",
                ActionType = "Ekleme",
                Timestamp = DateTime.UtcNow.AddHours(3)
            });

            await _context.SaveChangesAsync();

            return Ok(room);
        }

        /// <summary>
        /// Var olan bir odanın bilgilerini günceller.
        /// </summary>
        [HttpPut("rooms/{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateRoom(int id, [FromBody] RoomRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Oda adı boş olamaz." });

            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == id);
            if (room == null)
                return NotFound(new { message = $"ID'si {id} olan oda bulunamadı." });

            var floorExists = await _context.Floors.AnyAsync(f => f.Id == request.FloorId);
            if (!floorExists)
                return NotFound(new { message = "Seçilen kat bulunamadı." });

            room.Name = request.Name;
            room.FloorId = request.FloorId;

            var currentUsername = User.Identity?.Name ?? "Sistem";
            await _context.SystemLogs.AddAsync(new SystemLog
            {
                Username = currentUsername,
                Action = "Oda Güncellendi",
                Details = $"ID:{id} odası güncellendi.",
                ActionType = "Güncelleme",
                Timestamp = DateTime.UtcNow.AddHours(3)
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "Oda başarıyla güncellendi!", room });
        }

        /// <summary>
        /// Odayı ve içindeki tüm cihazları siler (confirm=true parametresi gerektirir).
        /// </summary>
        [HttpDelete("rooms/{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeleteRoom(int id, [FromQuery] bool confirm = false)
        {
            var room = await _context.Rooms
                .Include(r => r.Devices)
                    .ThenInclude(d => d.Ports)
                .Include(r => r.Devices)
                    .ThenInclude(d => d.Credential)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
                return NotFound(new { message = $"ID'si {id} olan oda bulunamadı." });

            var devices = room.Devices.ToList();
            var deviceIds = devices.Select(d => d.Id).ToList();
            var ports = devices.SelectMany(d => d.Ports).ToList();
            var credentials = devices.Where(d => d.Credential != null).Select(d => d.Credential!).ToList();
            var connections = await FindRelatedConnectionsAsync(deviceIds);

            if (!confirm)
            {
                return Conflict(new
                {
                    message = $"'{room.Name}' silinirse aşağıdaki kayıtlar da kalıcı olarak silinecek. Onaylamak için isteği ?confirm=true ile tekrar gönderin.",
                    willDelete = new
                    {
                        Devices = devices.Count,
                        Ports = ports.Count,
                        Credentials = credentials.Count,
                        Connections = connections.Count
                    }
                });
            }

            _context.Connections.RemoveRange(connections);
            _context.Rooms.Remove(room);

            var currentUsername = User.Identity?.Name ?? "Sistem";
            await _context.SystemLogs.AddAsync(new SystemLog
            {
                Username = currentUsername,
                Action = "Oda Silindi",
                Details = $"'{room.Name}' (ID:{id}) silindi. Birlikte silinenler: {devices.Count} cihaz, {ports.Count} port, {credentials.Count} credential, {connections.Count} bağlantı.",
                ActionType = "Silme",
                Timestamp = DateTime.UtcNow.AddHours(3)
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = $"'{room.Name}' ve bağlı tüm kayıtlar başarıyla silindi." });
        }

        // =========================================================================
        // 4. CİHAZ (DEVICE) İŞLEMLERİ
        // =========================================================================

        /// <summary>
        /// Cihaz adında veya MAC adresinde arama yapar.
        /// </summary>
        [HttpGet("devices/search")]
        public async Task<IActionResult> SearchDevices([FromQuery] string? query)
        {
            var devicesQuery = _context.Devices
                .AsNoTracking()
                .Include(d => d.Ports)
                .Include(d => d.Credential)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                devicesQuery = devicesQuery.Where(d =>
                    d.Name.Contains(query) ||
                    (d.MacAddress != null && d.MacAddress.Contains(query)));
            }

            var devices = await devicesQuery.ToListAsync();

            var results = devices.Select(d => new
            {
                d.Id,
                d.Name,
                d.DeviceType,
                d.Status,
                d.Model,
                d.MacAddress,
                d.SerialNumber,
                d.RoomId,
                Ports = d.Ports.Select(p => new { p.Id, p.PortNumber, p.Status }),
                SecurityInfo = BuildSecurityInfo(d.Credential)
            });

            return Ok(results);
        }

        /// <summary>
        /// Cihaz detaylarını ve portlarını getirir.
        /// </summary>
        [HttpGet("devices/{id}")]
        public async Task<IActionResult> GetDeviceDetails(int id)
        {
            var device = await _context.Devices
                .AsNoTracking()
                .Include(d => d.Ports)
                .Include(d => d.Credential)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (device == null) return NotFound(new { message = "Cihaz bulunamadı." });

            var response = new
            {
                device.Id,
                device.Name,
                device.DeviceType,
                device.Status,
                device.Model,
                device.MacAddress,
                device.SerialNumber,
                SecurityInfo = BuildSecurityInfo(device.Credential),
                Ports = device.Ports.Select(p => new { p.Id, p.PortNumber, p.Status })
            };

            return Ok(response);
        }

        /// <summary>
        /// Yeni bir cihaz tanımlar.
        /// </summary>
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
                DeviceType = request.DeviceType!.Value.ToDisplayString(),
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
                Timestamp = DateTime.UtcNow.AddHours(3)
            };
            await _context.SystemLogs.AddAsync(log);

            await _context.SaveChangesAsync();

            return Ok(newDevice);
        }

        /// <summary>
        /// Var olan bir cihazın tüm detaylarını günceller.
        /// </summary>
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
            existingDevice.DeviceType = request.DeviceType!.Value.ToDisplayString();
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
                Timestamp = DateTime.UtcNow.AddHours(3)
            };
            await _context.SystemLogs.AddAsync(log);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cihaz başarıyla güncellendi!", device = existingDevice });
        }

        /// <summary>
        /// Cihazı sistemden kaldırır.
        /// </summary>
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
                Timestamp = DateTime.UtcNow.AddHours(3)
            };
            await _context.SystemLogs.AddAsync(log);

            await _context.SaveChangesAsync();

            return Ok(new { message = $"'{device.Name}' başarıyla silindi." });
        }

        // =========================================================================
        // 5. DENETİM LOGLARI (SYSTEM LOGS)
        // =========================================================================

        /// <summary>
        /// Son 50 Sistem Logunu Getirir.
        /// </summary>
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

        // =========================================================================
        // HELPER METHOD(S)
        // =========================================================================

        private object BuildSecurityInfo(Credential? credential)
        {
            bool isSuperAdmin = User.IsInRole("SuperAdmin");

            if (isSuperAdmin && credential != null)
            {
                return new
                {
                    IpAddress = credential.IpAddress,
                    Username = credential.Username,
                    Password = credential.Password
                };
            }

            return new
            {
                IpAddress = "GİZLİ (Yetki Yok)",
                Username = "GİZLİ (Yetki Yok)",
                Password = "GİZLİ (Yetki Yok)"
            };
        }

        private async Task<List<Connection>> FindRelatedConnectionsAsync(List<int> deviceIds)
        {
            if (deviceIds.Count == 0) return new List<Connection>();

            return await _context.Connections
                .Where(c =>
                    _context.Ports.Any(p => p.Id == c.SourcePortId && deviceIds.Contains(p.DeviceId))
                    || deviceIds.Contains(c.TargetDeviceId))
                .ToListAsync();
        }
    }

    // =========================================================================
    // DTO / İSTEK MODELLERİ (REQUEST MODELS)
    // =========================================================================

    public class DeviceRequest
    {
        [Required(ErrorMessage = "Cihaz adı zorunludur.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Cihaz türü seçilmesi zorunludur.")]
        public NetworkDeviceType? DeviceType { get; set; }

        [Required(ErrorMessage = "Durum bilgisi zorunludur.")]
        [MaxLength(50)]
        public string Status { get; set; } = "Active";

        [MaxLength(150)]
        public string? Model { get; set; }

        [MaxLength(100)]
        public string? MacAddress { get; set; }

        [MaxLength(100)]
        public string? SerialNumber { get; set; }

        [Required(ErrorMessage = "Cihazın bağlı olduğu oda (RoomId) zorunludur.")]
        [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir RoomId belirtilmelidir.")]
        public int RoomId { get; set; }
    }

    public class FacilityRequest
    {
        public string Name { get; set; } = string.Empty;
        public string FacilityType { get; set; } = "Hizmet Binası";
        public string? Address { get; set; }
    }

    public class FloorRequest
    {
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public int FacilityId { get; set; }
    }

    public class RoomRequest
    {
        public string Name { get; set; } = string.Empty;
        public int FloorId { get; set; }
    }
}