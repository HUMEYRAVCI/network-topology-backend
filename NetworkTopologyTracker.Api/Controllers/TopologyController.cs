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
    /// <summary>
    /// Ağ Topolojisi ve Envanter Yönetimi Ana Denetleyicisi (Core Topology Controller).
    /// Kurumun hiyerarşik ağ mimarisini oluşturan Bina (<see cref="Facility"/>), Kat (<see cref="Floor"/>),
    /// Oda (<see cref="Room"/>), Cihaz (<see cref="Device"/>), Port (<see cref="Port"/>) ve Bağlantı (<see cref="Connection"/>)
    /// varlıkları üzerindeki tüm CRUD (Oluşturma, Okuma, Güncelleme, Silme) işlemlerini ve topoloji ağacı sorgularını yönetir.
    /// 
    /// GÜVENLİK VE YETKİLENDİRME MİMARİSİ:
    /// - Sınıf seviyesindeki <see cref="AuthorizeAttribute"/> sayesinde tüm uç noktalar geçerli bir JWT Token gerektirir.
    /// - Okuma (GET) uç noktaları hem "Viewer" hem de "SuperAdmin" rollerine açıktır; ancak cihazların hassas erişim
    ///   bilgileri (<see cref="Credential"/>) <see cref="BuildSecurityInfo"/> metodu ile "Viewer" rolünden gizlenir.
    /// - Veri değiştiren (POST, PUT, DELETE) tüm uç noktalar <c>[Authorize(Roles = "SuperAdmin")]</c> ile korunmaktadır.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TopologyController : ControllerBase
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Veritabanı bağlamını (<see cref="AppDbContext"/>) Dependency Injection ile alır.
        /// </summary>
        public TopologyController(AppDbContext context)
        {
            _context = context;
        }

        // =========================================================================
        // 1. BİNA / TESİS (FACILITY) İŞLEMLERİ
        // =========================================================================

        /// <summary>
        /// Sistemde kayıtlı tüm tesis/binaların özet listesini getirir.
        /// Rota: GET /api/Topology/facilities
        /// </summary>
        [HttpGet("facilities")]
        public async Task<IActionResult> GetFacilities()
        {
            // AsNoTracking() ile EF Core değişiklik izleme (Change Tracker) maliyeti devre dışı bırakılarak
            // salt-okunur sorgu performansı artırılır; yalnızca arayüzün ihtiyaç duyduğu alanlar projekte edilir.
            var facilities = await _context.Facilities
                .AsNoTracking()
                .Select(f => new { f.Id, f.Name, f.FacilityType, f.Address })
                .ToListAsync();

            return Ok(facilities);
        }

        /// <summary>
        /// Sisteme yeni bir fiziksel bina / tesis tanımlar ve işlemi denetim loglarına kaydeder.
        /// Rota: POST /api/Topology/facilities
        /// Yetki: Yalnızca "SuperAdmin"
        /// </summary>
        [HttpPost("facilities")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CreateFacility([FromBody] FacilityRequest request)
        {
            // İstek gövdesi ve zorunlu bina adı doğrulaması.
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Bina adı boş olamaz." });

            // Bina türü belirtilmemişse varsayılan olarak "Hizmet Binası" atanır; saat Türkiye saatine (UTC+3) ayarlanır.
            var facility = new Facility
            {
                Name = request.Name,
                FacilityType = string.IsNullOrWhiteSpace(request.FacilityType) ? "Hizmet Binası" : request.FacilityType,
                Address = request.Address,
                CreatedAt = DateTime.UtcNow.AddHours(3)
            };

            await _context.Facilities.AddAsync(facility);

            // İşlemi gerçekleştiren yöneticiyi JWT Token içerisindeki Name claim'inden okuyarak log kaydı oluştur.
            var currentUsername = User.Identity?.Name ?? "Sistem";
            await _context.SystemLogs.AddAsync(new SystemLog
            {
                Username = currentUsername,
                Action = "Bina Eklendi",
                Details = $"'{request.Name}' isimli yeni tesis eklendi.",
                ActionType = "Ekleme",
                Timestamp = DateTime.UtcNow.AddHours(3)
            });

            // Hem yeni tesisi hem de denetim logunu tek bir veritabanı işlemi (Transaction) içinde kaydet.
            await _context.SaveChangesAsync();

            return Ok(facility);
        }

        /// <summary>
        /// Seçilen bir tesise (facilityId) ait tüm katları, odaları, cihazları, portları,
        /// rol bazlı maskelenmiş güvenlik bilgilerini ve cihazlar arası kablo bağlantı haritasını (Topoloji Ağacı) getirir.
        /// Rota: GET /api/Topology/facilities/{facilityId}/topology
        /// </summary>
        /// <param name="facilityId">Topolojisi görüntülenecek binanın ID değeri.</param>
        [HttpGet("facilities/{facilityId}/topology")]
        public async Task<IActionResult> GetTopology(int facilityId)
        {
            // 1. ADIM: Seçilen binadaki tüm odaları, bağlı oldukları kat bilgisiyle (Floor),
            // içlerindeki cihazlarla (Devices), cihazların portlarıyla (Ports) ve erişim bilgileriyle (Credential)
            // Eager Loading (Include / ThenInclude) kullanarak tek seferde çek.
            var rooms = await _context.Rooms
                .AsNoTracking()
                .Include(r => r.Floor)
                .Where(r => r.Floor != null && r.Floor.FacilityId == facilityId)
                .Include(r => r.Devices)
                    .ThenInclude(d => d.Ports)
                .Include(r => r.Devices)
                    .ThenInclude(d => d.Credential)
                .ToListAsync();

            // 2. ADIM: Kaynak portu (SourcePort) bu binadaki bir cihaza ait olan tüm fiziksel/kablosuz
            // bağlantıları (Connections) çekerek topoloji grafiğinin kenarlarını (Edges) oluştur.
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

            // 3. ADIM: İstemcinin (Frontend topoloji haritası bileşeninin) doğrudan görselleştirebileceği
            // hiyerarşik JSON yanıt modelini inşa et. Cihaz şifreleri BuildSecurityInfo() ile rol kontrolünden geçirilir.
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
        /// Var olan bir binanın ad, tür ve adres bilgilerini günceller.
        /// Rota: PUT /api/Topology/facilities/{id}
        /// Yetki: Yalnızca "SuperAdmin"
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

            // Alanları güncelle (Yeni FacilityType boş gönderilmişse mevcut türü koru).
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
        /// Bir binayı ve altındaki tüm hiyerarşik alt öğeleri (Katlar, Odalar, Cihazlar, Portlar, Kimlik Bilgileri, Bağlantılar)
        /// güvenli iki aşamalı onay mekanizmasıyla (<c>?confirm=true</c>) siler.
        /// Rota: DELETE /api/Topology/facilities/{id}?confirm=true
        /// Yetki: Yalnızca "SuperAdmin"
        /// </summary>
        /// <param name="id">Silinecek binanın ID değeri.</param>
        /// <param name="confirm">False ise silme yapılmaz, etkilenecek alt kayıtların özeti 409 Conflict ile döndürülür.</param>
        [HttpDelete("facilities/{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeleteFacility(int id, [FromQuery] bool confirm = false)
        {
            // Silinecek binanın altındaki tüm hiyerarşik ağacı (Kat -> Oda -> Cihaz -> Port / Credential) belleğe yükle.
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

            // Etki analizi (Impact Analysis) ve loglama için alt öğeleri düzleştirerek (Flatten) listele.
            var floors = facility.Floors.ToList();
            var rooms = floors.SelectMany(fl => fl.Rooms).ToList();
            var devices = rooms.SelectMany(r => r.Devices).ToList();
            var deviceIds = devices.Select(d => d.Id).ToList();
            var ports = devices.SelectMany(d => d.Ports).ToList();
            var credentials = devices.Where(d => d.Credential != null).Select(d => d.Credential!).ToList();
            var connections = await FindRelatedConnectionsAsync(deviceIds);

            // 1. AŞAMA (ONAY KONTROLÜ): Yönetici henüz ?confirm=true göndermemişse veriyi silme;
            // hangi alt kayıtlardan kaç adet silineceğini HTTP 409 (Conflict) ile bildirerek onay iste.
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

            // 2. AŞAMA (KALICI SİLME):
            // Connection tablosunda DeleteBehavior.Restrict tanımlı olduğu için yabancı anahtar hatası almamak adına
            // önce cihazlara bağlı kablolar (Connections) silinir, ardından bina silinir (diğer alt tablolar Cascade ile silinir).
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
        /// Belirtilen binaya (FacilityId) yeni bir kat ekler.
        /// Rota: POST /api/Topology/floors
        /// Yetki: Yalnızca "SuperAdmin"
        /// </summary>
        [HttpPost("floors")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CreateFloor([FromBody] FloorRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Kat adı boş olamaz." });

            // Yabancı anahtar doğrulaması: Katın ekleneceği üst bina veritabanında mevcut mu?
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
        /// Var olan bir katın adını, seviyesini (Level) veya bağlı olduğu binayı günceller.
        /// Rota: PUT /api/Topology/floors/{id}
        /// Yetki: Yalnızca "SuperAdmin"
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

            // Hedef binanın varlığını kontrol et.
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
        /// Bir katı ve altındaki tüm odaları/cihazları iki aşamalı onay mekanizmasıyla (<c>?confirm=true</c>) siler.
        /// Rota: DELETE /api/Topology/floors/{id}?confirm=true
        /// Yetki: Yalnızca "SuperAdmin"
        /// </summary>
        [HttpDelete("floors/{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeleteFloor(int id, [FromQuery] bool confirm = false)
        {
            // Katın altındaki tüm oda, cihaz, port ve kimlik bilgilerini belleğe yükle.
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

            // Silinecek alt varlıkların sayılarını hesapla.
            var rooms = floor.Rooms.ToList();
            var devices = rooms.SelectMany(r => r.Devices).ToList();
            var deviceIds = devices.Select(d => d.Id).ToList();
            var ports = devices.SelectMany(d => d.Ports).ToList();
            var credentials = devices.Where(d => d.Credential != null).Select(d => d.Credential!).ToList();
            var connections = await FindRelatedConnectionsAsync(deviceIds);

            // Onay verilmemişse silinecek envanter özetini 409 Conflict ile döndür.
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

            // Önce Restrict kısıtlamasına sahip bağlantıları, ardından katı sil.
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
        // 3. ODA / BİRİM (ROOM) İŞLEMLERİ
        // =========================================================================

        /// <summary>
        /// Seçilen kata (FloorId) yeni bir oda veya birim ekler.
        /// Rota: POST /api/Topology/rooms
        /// Yetki: Yalnızca "SuperAdmin"
        /// </summary>
        [HttpPost("rooms")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CreateRoom([FromBody] RoomRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Oda adı boş olamaz." });

            // Odanın ekleneceği katın varlığını doğrula.
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
        /// Var olan bir odanın adını veya bulunduğu katı günceller.
        /// Rota: PUT /api/Topology/rooms/{id}
        /// Yetki: Yalnızca "SuperAdmin"
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
        /// Bir odayı ve içindeki tüm cihazları iki aşamalı onay mekanizmasıyla (<c>?confirm=true</c>) siler.
        /// Rota: DELETE /api/Topology/rooms/{id}?confirm=true
        /// Yetki: Yalnızca "SuperAdmin"
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

            // Onay parametresi false ise silinecek bağlı kayıt sayılarını döndür.
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

            // Önce cihazların dahil olduğu bağlantıları, ardından odayı sil.
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
        // 4. AĞ VE UÇ CİHAZ (DEVICE) İŞLEMLERİ
        // =========================================================================

        /// <summary>
        /// Tüm tesislerdeki cihazlar arasında cihaz adına (Hostname) veya MAC adresine göre arama yapar.
        /// Arama metni (query) boş bırakılırsa tüm cihazları listeler.
        /// Rota: GET /api/Topology/devices/search?query=...
        /// </summary>
        [HttpGet("devices/search")]
        public async Task<IActionResult> SearchDevices([FromQuery] string? query)
        {
            // Sorgu ağacını (IQueryable) hazırla; portları ve erişim bilgilerini dahil et.
            var devicesQuery = _context.Devices
                .AsNoTracking()
                .Include(d => d.Ports)
                .Include(d => d.Credential)
                .AsQueryable();

            // Arama parametresi doluysa Name veya MacAddress alanlarında kısmi eşleşme (LIKE %query%) uygula.
            if (!string.IsNullOrWhiteSpace(query))
            {
                devicesQuery = devicesQuery.Where(d =>
                    d.Name.Contains(query) ||
                    (d.MacAddress != null && d.MacAddress.Contains(query)));
            }

            var devices = await devicesQuery.ToListAsync();

            // Sonuçları rol bazlı güvenlik maskelemesi (BuildSecurityInfo) uygulayarak döndür.
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
        /// Belirli bir cihazın (id) tüm donanım özelliklerini, port listesini ve yetkiye bağlı erişim bilgilerini getirir.
        /// Rota: GET /api/Topology/devices/{id}
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
        /// Seçilen bir odaya (RoomId) yeni bir ağ cihazı ekler.
        /// Rota: POST /api/Topology/devices
        /// Yetki: Yalnızca "SuperAdmin"
        /// </summary>
        [HttpPost("devices")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CreateDevice([FromBody] DeviceRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Cihaz bilgileri boş olamaz." });
            }

            // Cihazın konumlandırılacağı odanın veritabanında mevcut olup olmadığını kontrol et.
            var room = await _context.Rooms.FindAsync(request.RoomId);
            if (room == null)
            {
                return BadRequest(new { message = "Seçilen oda veritabanında bulunamadı." });
            }

            // DTO'dan gelen NetworkDeviceType enum değerini ToDisplayString() ile standart metin karşılığına çevir.
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
        /// Var olan bir cihazın donanım, durum ve oda konum bilgilerini günceller.
        /// Rota: PUT /api/Topology/devices/{id}
        /// Yetki: Yalnızca "SuperAdmin"
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

            // Mevcut cihazın özelliklerini istek modelindeki yeni değerlerle güncelle.
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
        /// Belirtilen cihazı sistemden kalıcı olarak siler ve denetim logu oluşturur.
        /// Rota: DELETE /api/Topology/devices/{id}
        /// Yetki: Yalnızca "SuperAdmin"
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
        /// Sistemde gerçekleşen son 50 denetim logunu en güncelden en eskiye doğru getirir.
        /// Rota: GET /api/Topology/logs
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
        // YARDIMCI METOTLAR (HELPER METHODS)
        // =========================================================================

        /// <summary>
        /// Rol Tabanlı Veri Maskeleme Yardımcı Metodu:
        /// İsteği yapan kullanıcının JWT Token'ındaki rolü "SuperAdmin" ise cihazın gerçek IP, kullanıcı adı
        /// ve şifre bilgilerini döndürür. Aksi halde ("Viewer" rolü veya Credential kaydı yoksa)
        /// siber güvenlik gereği bu alanları "GİZLİ (Yetki Yok)" metniyle maskeler.
        /// </summary>
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

        /// <summary>
        /// Silinecek cihaz listesine (deviceIds) bağlı olan tüm gelen (TargetDeviceId) ve giden (SourcePortId)
        /// ağ bağlantılarını (<see cref="Connection"/>) tespit eder.
        /// MS SQL Server'da Connection ilişkileri DeleteBehavior.Restrict olarak tanımlandığı için,
        /// üst varlıklar (Bina/Kat/Oda) silinmeden önce bu bağlantılar bulunup temizlenmelidir.
        /// </summary>
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
    // DTO / İSTEK MODELLERİ (DATA TRANSFER OBJECTS / REQUEST MODELS)
    // =========================================================================

    /// <summary>
    /// Yeni cihaz ekleme (POST) ve mevcut cihazı güncelleme (PUT) isteklerinde kullanılan,
    /// DataAnnotations doğrulama (Validation) kurallarına sahip DTO sınıfı.
    /// </summary>
    public class DeviceRequest
    {
        /// <summary>Cihazın topolojide görünecek adı (Zorunlu, maks. 100 karakter).</summary>
        [Required(ErrorMessage = "Cihaz adı zorunludur.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Cihazın türü (Zorunlu). Nullable enum olarak tanımlanmıştır; böylece istemci bu alanı
        /// göndermezse varsayılan ilk enum değerine (0 = Router) düşmek yerine [Required] doğrulamasına takılır.
        /// </summary>
        [Required(ErrorMessage = "Cihaz türü seçilmesi zorunludur.")]
        public NetworkDeviceType? DeviceType { get; set; }

        /// <summary>Cihazın çalışma durumu (Örn: "Active", "Inactive", "Maintenance").</summary>
        [Required(ErrorMessage = "Durum bilgisi zorunludur.")]
        [MaxLength(50)]
        public string Status { get; set; } = "Active";

        /// <summary>Cihazın marka ve model bilgisi (İsteğe bağlı).</summary>
        [MaxLength(150)]
        public string? Model { get; set; }

        /// <summary>Cihazın fiziksel MAC adresi (İsteğe bağlı).</summary>
        [MaxLength(100)]
        public string? MacAddress { get; set; }

        /// <summary>Cihazın üretici seri numarası (İsteğe bağlı).</summary>
        [MaxLength(100)]
        public string? SerialNumber { get; set; }

        /// <summary>Cihazın ekleneceği odanın veritabanı ID değeri (1 veya daha büyük olmalıdır).</summary>
        [Required(ErrorMessage = "Cihazın bağlı olduğu oda (RoomId) zorunludur.")]
        [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir RoomId belirtilmelidir.")]
        public int RoomId { get; set; }
    }

    /// <summary>
    /// Bina oluşturma ve güncelleme istekleri için kullanılan DTO sınıfı.
    /// </summary>
    public class FacilityRequest
    {
        public string Name { get; set; } = string.Empty;
        public string FacilityType { get; set; } = "Hizmet Binası";
        public string? Address { get; set; }
    }

    /// <summary>
    /// Kat oluşturma ve güncelleme istekleri için kullanılan DTO sınıfı.
    /// </summary>
    public class FloorRequest
    {
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public int FacilityId { get; set; }
    }

    /// <summary>
    /// Oda oluşturma ve güncelleme istekleri için kullanılan DTO sınıfı.
    /// </summary>
    public class RoomRequest
    {
        public string Name { get; set; } = string.Empty;
        public int FloorId { get; set; }
    }
}
