using Microsoft.EntityFrameworkCore;
using NetworkTopologyTracker.Api.Entities;

namespace NetworkTopologyTracker.Api.Data
{
    public static class SeedData
    {
        public static async Task Initialize(AppDbContext context)
        {
            // Eğer veritabanında zaten veri varsa tekrar ekleme yapma
            if (await context.Facilities.AnyAsync())
            {
                return;
            }

            // 1. ADIM: Örnek Tesis Ekleme
            var facility = new Facility
            {
                Name = "Talas Belediyesi Merkez Kütüphanesi",
                Address = "Yenidoğan Mh., Talas, Kayseri"
            };
            await context.Facilities.AddAsync(facility);
            await context.SaveChangesAsync(); // Id oluşması için kaydediyoruz

            // 2. ADIM: Örnek Oda Ekleme
            var room = new Room
            {
                Name = "Zemin Kat - Ana Sistem Odası",
                FloorInfo = "Zemin Kat",
                FacilityId = facility.Id
            };
            await context.Rooms.AddAsync(room);
            await context.SaveChangesAsync();

            // 3. ADIM: Örnek Cihazlar Ekleme (Router ve Switch)
            var router = new Device
            {
                Name = "TALAS-CORE-ROUTER",
                DeviceType = "Router",
                Status = "Active",
                Brand = "Cisco",
                Model = "ISR 4331",
                MacAddress = "00:AA:BB:CC:DD:EE",
                SerialNumber = "SN-ROUTER123",
                RoomId = room.Id
            };

            var edgeSwitch = new Device
            {
                Name = "TALAS-KAT1-SWITCH",
                DeviceType = "Switch",
                Status = "Active",
                Brand = "Cisco",
                Model = "Catalyst 2960X",
                MacAddress = "00:AA:BB:CC:DD:FF",
                SerialNumber = "SN-SWITCH456",
                RoomId = room.Id
            };

            await context.Devices.AddRangeAsync(router, edgeSwitch);
            await context.SaveChangesAsync();

            // 4. ADIM: Hassas Giriş Bilgilerini (Credential) Kilitli Odaya Ekleme
            var routerCred = new Credential
            {
                IpAddress = "192.168.1.1",
                Username = "talas_admin",
                Password = "SuperSecretRouterPassword!",
                DeviceId = router.Id
            };

            var switchCred = new Credential
            {
                IpAddress = "192.168.1.2",
                Username = "talas_switch_admin",
                Password = "SecureSwitchPassword456!",
                DeviceId = edgeSwitch.Id
            };

            await context.Credentials.AddRangeAsync(routerCred, switchCred);

            // 5. ADIM: Portları Oluşturma
            var routerPort = new Port
            {
                PortNumber = "GigabitEthernet0/0/1",
                Status = "Connected",
                DeviceId = router.Id
            };

            var switchPort = new Port
            {
                PortNumber = "GigabitEthernet1/0/24",
                Status = "Connected",
                DeviceId = edgeSwitch.Id
            };

            await context.Ports.AddRangeAsync(routerPort, switchPort);
            await context.SaveChangesAsync();

            // 6. ADIM: Topoloji Çizgisi (Kablo Bağlantısı) Oluşturma
            // Router'ın portundan çıkan kablo, Switch cihazına gidiyor.
            var connection = new Connection
            {
                ConnectionType = "Cat6",
                SourcePortId = routerPort.Id,
                TargetDeviceId = edgeSwitch.Id
            };

            await context.Connections.AddAsync(connection);
            await context.SaveChangesAsync();
        }
    }
}