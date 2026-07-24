using Microsoft.EntityFrameworkCore;
using NetworkTopologyTracker.Api.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
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
                superAdmin.Password = hasher.HashPassword(superAdmin, "password123!");

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

            // Eğer veritabanında zaten bina verisi varsa seed verilerini tekrar yükleme
            if (await context.Facilities.AnyAsync())
            {
                return;
            }

            // 1. ADIM: Gerçek Talas Belediyesi Mekanlarının (Binalarının) Eklenmesi
            var merkezBina = new Facility { Name = "Talas Belediyesi Merkez Binası", FacilityType = "Merkez Bina", Address = "Yenidoğan Mah., Pazar Cad. No:10, Talas, Kayseri" };
            var eskiBina = new Facility { Name = "Eski Belediye Binası", FacilityType = "Tarihi Tesis", Address = "Tablakaya Mah., Cumhuriyet Meydanı, Talas, Kayseri" };
            var kulturMerkezi = new Facility { Name = "Talas Bld. Kültür Merkezi ve 7/24 Kütüphane", FacilityType = "Kütüphane & Kültür", Address = "Yenidoğan Mah., Şaban Yılmaz Cad. No:1, Talas, Kayseri" };
            var altintepeKutuphane = new Facility { Name = "Altıntepe Semt Kütüphanesi", FacilityType = "Kütüphane", Address = "Kiçiköy Mah., Altıntepe, Talas, Kayseri" };
            var abdulhamidhanKutuphane = new Facility { Name = "Abdülhamidhan Semt Kütüphanesi", FacilityType = "Kütüphane", Address = "Mevlana Mah., Talas, Kayseri" };
            var guldalKutuphane = new Facility { Name = "Hafize-Ahmet Güldal Semt Kütüphanesi", FacilityType = "Kütüphane", Address = "Bahçelievler Mah., Talas, Kayseri" };
            var santiyeTesis = new Facility { Name = "Talas Belediyesi Fen İşleri Şantiyesi", FacilityType = "Teknik Tesis", Address = "Yenidoğan Mah., Ali Dağı Yolu Üzeri, Talas, Kayseri" };
            var alidagiTesis = new Facility { Name = "Ali Dağı 360 Tesisleri", FacilityType = "Açık Tesis", Address = "Han Mah., Ali Dağı Mevkii, Talas, Kayseri" };
            var sosyalTesis = new Facility { Name = "Talas Kadın Kültür ve Sosyal Tesisi", FacilityType = "Sosyal Tesis", Address = "Bahçelievler Mah., Atatürk Bulvarı No:42, Talas, Kayseri" };

            await context.Facilities.AddRangeAsync(
                merkezBina, eskiBina, kulturMerkezi, altintepeKutuphane, 
                abdulhamidhanKutuphane, guldalKutuphane, santiyeTesis, alidagiTesis, sosyalTesis
            );
            await context.SaveChangesAsync();

            // 2. ADIM: Katları Tanımlama (Floors)
            // -- Merkez Bina Katları
            var m_zemin = new Floor { Name = "Zemin Kat", Level = 0, FacilityId = merkezBina.Id };
            var m_kat1 = new Floor { Name = "1. Kat (Başkanlık)", Level = 1, FacilityId = merkezBina.Id };
            var m_kat2 = new Floor { Name = "2. Kat (İmar ve Şehircilik)", Level = 2, FacilityId = merkezBina.Id };

            // -- Eski Belediye Binası
            var e_zemin = new Floor { Name = "Zemin Kat", Level = 0, FacilityId = eskiBina.Id };

            // -- Kültür Merkezi ve 7/24 Kütüphane Gerçek Katları (4 Katlı Yapı)
            var k_kat1 = new Floor { Name = "1. Kat (Kurslar & Atölyeler)", Level = 1, FacilityId = kulturMerkezi.Id };
            var k_kat2 = new Floor { Name = "2. Kat (Dijital Kütüphane & Lab)", Level = 2, FacilityId = kulturMerkezi.Id };
            var k_kat3 = new Floor { Name = "3. Kat (7/24 Ders Çalışma & Cafe)", Level = 3, FacilityId = kulturMerkezi.Id };
            var k_kat4 = new Floor { Name = "4. Kat (Akademisyen Çalışma Alanı)", Level = 4, FacilityId = kulturMerkezi.Id };

            // -- Şantiye Tesisi
            var s_zemin = new Floor { Name = "Zemin Kat (İdari Bina)", Level = 0, FacilityId = santiyeTesis.Id };

            await context.Floors.AddRangeAsync(
                m_zemin, m_kat1, m_kat2, e_zemin, 
                k_kat1, k_kat2, k_kat3, k_kat4, s_zemin
            );
            await context.SaveChangesAsync();

            // 3. ADIM: Odaları Tanımlama (Rooms)
            // -- Merkez Bina Odaları
            var r_merkez_sistem = new Room { Name = "Ana Bilgi İşlem & Sistem Odası", FloorId = m_zemin.Id };
            var r_merkez_baskan = new Room { Name = "Başkanlık Makamı Sistem Kabini", FloorId = m_kat1.Id };

            // -- Eski Bina Oda
            var r_eski_sistem = new Room { Name = "Tarihi Bina Sistem Kabini Odası", FloorId = e_zemin.Id };

            // -- Kültür Merkezi Odaları
            var r_kultur_lab = new Room { Name = "Bilgisayar Laboratuvarı ve Dijital Araştırma Odası", FloorId = k_kat2.Id };
            var r_kultur_engelli = new Room { Name = "Engelli Çalışma ve Erişim Odası", FloorId = k_kat2.Id };
            var r_kultur_okuma = new Room { Name = "7/24 Sessiz Okuma Salonu Sistem Pano Odası", FloorId = k_kat3.Id };
            var r_kultur_akademik = new Room { Name = "Akademisyen Katı Ağ Dağıtım Odası", FloorId = k_kat4.Id };

            // -- Şantiye Oda
            var r_santiye_sistem = new Room { Name = "Şantiye Saha Kontrol ve Sistem Odası", FloorId = s_zemin.Id };

            await context.Rooms.AddRangeAsync(
                r_merkez_sistem, r_merkez_baskan, r_eski_sistem, 
                r_kultur_lab, r_kultur_engelli, r_kultur_okuma, r_kultur_akademik, r_santiye_sistem
            );
            await context.SaveChangesAsync();

            // 4. ADIM: Ağ Cihazları Ekleme (Devices - Brand ve Model Birleştirildi)
            // -- Merkez Bina Cihazları
            var coreRouter = new Device
            {
                Name = "TALAS-CORE-ROUTER",
                DeviceType = "Router",
                Status = "Active",
                Model = "Cisco ISR 4451",
                MacAddress = "00:AA:BB:CC:DD:EE",
                SerialNumber = "SN-CORE-101",
                RoomId = r_merkez_sistem.Id
            };

            var coreSwitch = new Device
            {
                Name = "TALAS-CORE-SWITCH-01",
                DeviceType = "Switch",
                Status = "Active",
                Model = "Cisco Catalyst 9300 48-Port",
                MacAddress = "00:AA:BB:CC:DD:FF",
                SerialNumber = "SN-CSW-102",
                RoomId = r_merkez_sistem.Id
            };

            var mainFirewall = new Device
            {
                Name = "TALAS-FIREWALL-MAIN",
                DeviceType = "Firewall",
                Status = "Active",
                Model = "Fortinet FortiGate 100F",
                MacAddress = "00:AA:BB:CC:DD:11",
                SerialNumber = "SN-FW-103",
                RoomId = r_merkez_sistem.Id
            };

            // -- Kültür Merkezi ve Kütüphane Cihazları
            var kutupPoeSwitch = new Device
            {
                Name = "TALAS-KUTUP-KAT2-POE-SW",
                DeviceType = "Switch",
                Status = "Active",
                Model = "Cisco Catalyst 3850 48-Port PoE",
                MacAddress = "00:18:63:84:45:10",
                SerialNumber = "SN-LIB-SW201",
                RoomId = r_kultur_lab.Id
            };

            var kutupWifiAp = new Device
            {
                Name = "TALAS-KUTUP-3KAT-WIFI-AP",
                DeviceType = "Access Point",
                Status = "Active",
                Model = "Aruba AP-535 Wi-Fi 6",
                MacAddress = "F4:2E:7F:11:99:AA",
                SerialNumber = "SN-LIB-AP301",
                RoomId = r_kultur_okuma.Id
            };

            var kutupFw = new Device
            {
                Name = "TALAS-KUTUP-AKADEMIK-FW",
                DeviceType = "Firewall",
                Status = "Active",
                Model = "Fortinet FortiGate 60F",
                MacAddress = "00:09:0F:77:88:99",
                SerialNumber = "SN-LIB-FW401",
                RoomId = r_kultur_akademik.Id
            };

            // -- Şantiye Cihazı
            var santiyeSwitch = new Device
            {
                Name = "TALAS-SANTIYE-OUTDOOR-SW",
                DeviceType = "Switch",
                Status = "Active",
                Model = "Rugged Industrial PoE Switch 16P",
                MacAddress = "00:11:22:33:44:55",
                SerialNumber = "SN-SAN-501",
                RoomId = r_santiye_sistem.Id
            };

            await context.Devices.AddRangeAsync(coreRouter, coreSwitch, mainFirewall, kutupPoeSwitch, kutupWifiAp, kutupFw, santiyeSwitch);
            await context.SaveChangesAsync();

            // 5. ADIM: Hassas Giriş Bilgilerini (Credential) Ekleme
            var routerCred = new Credential
            {
                IpAddress = "10.124.1.1",
                Username = "talas_admin",
                Password = "SuperSecretRouterPassword!",
                DeviceId = coreRouter.Id
            };

            var switchCred = new Credential
            {
                IpAddress = "10.124.1.2",
                Username = "talas_switch_admin",
                Password = "SecureSwitchPassword456!",
                DeviceId = coreSwitch.Id
            };

            var fwCred = new Credential
            {
                IpAddress = "10.124.1.254",
                Username = "talas_fw_admin",
                Password = "FortiGateSecurePass789!",
                DeviceId = mainFirewall.Id
            };

            await context.Credentials.AddRangeAsync(routerCred, switchCred, fwCred);

            // 6. ADIM: Portları Oluşturma
            var routerPort = new Port
            {
                PortNumber = "GigabitEthernet0/0/1",
                Status = "Connected",
                DeviceId = coreRouter.Id
            };

            var switchPort = new Port
            {
                PortNumber = "GigabitEthernet1/0/24",
                Status = "Connected",
                DeviceId = coreSwitch.Id
            };

            await context.Ports.AddRangeAsync(routerPort, switchPort);
            await context.SaveChangesAsync();

            // 7. ADIM: Topoloji Çizgisi (Kablo Bağlantısı) Oluşturma
            var connection = new Connection
            {
                ConnectionType = "Fiber Optic",
                SourcePortId = routerPort.Id,
                TargetDeviceId = coreSwitch.Id
            };

            await context.Connections.AddAsync(connection);
            await context.SaveChangesAsync();
        }
    }
}