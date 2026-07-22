using Microsoft.EntityFrameworkCore;
using NetworkTopologyTracker.Api.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;

namespace NetworkTopologyTracker.Api.Data
{
    public static class SeedData
    {
        public static async Task Initialize(AppDbContext context)
        {
            // 0. ADIM: Test Kullanıcılarını Güvenli Şifreyle Ekleme
            if (!await context.Users.AnyAsync())
            {
                var hasher = new PasswordHasher<User>();

                var superAdmin = new User
                {
                    FirstName = "Hümeyra",
                    LastName = "Avcı",
                    Username = "humeyra",
                    Role = "SuperAdmin"
                };
                superAdmin.Password = hasher.HashPassword(superAdmin, "password123");

                var viewer = new User
                {
                    FirstName = "Ahmet",
                    LastName = "Yılmaz",
                    Username = "ahmet_viewer",
                    Role = "Viewer"
                };
                viewer.Password = hasher.HashPassword(viewer, "viewer123");

                await context.Users.AddRangeAsync(superAdmin, viewer);
                await context.SaveChangesAsync();
            }

            // Eğer veritabanında zaten kurum/tesis verisi varsa hiyerarşik seed verilerini tekrar ekleme
            if (await context.Institutions.AnyAsync())
            {
                return;
            }

            // 1. ADIM: Kurum Tanımlama (Yeni Hiyerarşi - En Tepe Katman)
            var talasBelediyesi = new Institution
            {
                Name = "Talas Belediyesi (Merkez)",
                Description = "Kayseri Talas Belediyesi Ağ Topolojisi Yönetim Merkezi"
            };
            await context.Institutions.AddAsync(talasBelediyesi);
            await context.SaveChangesAsync();

            // 2. ADIM: Gerçek Talas Belediyesi Mekanlarının Eklenmesi
            var merkezBina = new Facility { Name = "Talas Belediyesi Merkez Binası", FacilityType = "Merkez Bina", InstitutionId = talasBelediyesi.Id, Address = "Yenidoğan Mah., Pazar Cad. No:10, Talas, Kayseri" };
            var eskiBina = new Facility { Name = "Eski Belediye Binası", FacilityType = "Tarihi Tesis", InstitutionId = talasBelediyesi.Id, Address = "Tablakaya Mah., Cumhuriyet Meydanı, Talas, Kayseri" };
            var kulturMerkezi = new Facility { Name = "Talas Bld. Kültür Merkezi ve 7/24 Kütüphane", FacilityType = "Kütüphane", InstitutionId = talasBelediyesi.Id, Address = "Yenidoğan Mah., Şaban Yılmaz Cad. No:1, Talas, Kayseri" };
            var altintepeKutuphane = new Facility { Name = "Altıntepe Semt Kütüphanesi", FacilityType = "Kütüphane", InstitutionId = talasBelediyesi.Id, Address = "Kiçiköy Mah., Altıntepe, Talas, Kayseri" };
            var abdulhamidhanKutuphane = new Facility { Name = "Abdülhamidhan Semt Kütüphanesi", FacilityType = "Kütüphane", InstitutionId = talasBelediyesi.Id, Address = "Mevlana Mah., Talas, Kayseri" };
            var guldalKutuphane = new Facility { Name = "Hafize-Ahmet Güldal Semt Kütüphanesi", FacilityType = "Kütüphane", InstitutionId = talasBelediyesi.Id, Address = "Bahçelievler Mah., Talas, Kayseri" };

            await context.Facilities.AddRangeAsync(merkezBina, eskiBina, kulturMerkezi, altintepeKutuphane, abdulhamidhanKutuphane, guldalKutuphane);
            await context.SaveChangesAsync();

            // 3. ADIM: Katları Tanımlama (Floors)
            var zeminKat = new Floor { Name = "Zemin Kat", Level = 0, FacilityId = merkezBina.Id };
            var kat1 = new Floor { Name = "1. Kat", Level = 1, FacilityId = merkezBina.Id };
            var kutuphaneZemin = new Floor { Name = "Zemin Kat", Level = 0, FacilityId = kulturMerkezi.Id };

            await context.Floors.AddRangeAsync(zeminKat, kat1, kutuphaneZemin);
            await context.SaveChangesAsync();

            // 4. ADIM: Odaları Tanımlama (Rooms)
            var room = new Room
            {
                Name = "Zemin Kat - Ana Sistem Odası",
                FloorId = zeminKat.Id
            };
            await context.Rooms.AddAsync(room);
            await context.SaveChangesAsync();

            // 5. ADIM: Örnek Cihazlar Ekleme
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

            // 6. ADIM: Hassas Giriş Bilgilerini (Credential) Ekleme
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

            // 7. ADIM: Portları Oluşturma
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

            // 8. ADIM: Topoloji Çizgisi (Kablo Bağlantısı) Oluşturma
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