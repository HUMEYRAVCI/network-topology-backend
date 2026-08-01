using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NetworkTopologyTracker.Api.Entities;

namespace NetworkTopologyTracker.Api.Data
{
        //   DbInitializer.cs  -> Facility/Floor/Room/Device/Port/Connection/Credential
    public static class DbInitializer
    {
        // =====================================================================
        // SAYAÇLAR & RASTGELE ÜRETİCİLER (MAC & Seri No)
        // =====================================================================
        private static int _macCounter = 100;
        private static int _serialCounter = 98234100;

        private static string GenerateMac()
        {
            _macCounter++;
            return $"00:1A:2C:3B:{(_macCounter / 256) % 256:X2}:{_macCounter % 256:X2}";
        }

        private static string GenerateSerial()
        {
            _serialCounter++;
            return $"SN-{_serialCounter}";
        }

        // =====================================================================
        // ANA GİRİŞ NOKTASI: Her şeyi temizler, 9 binayı baştan inşa eder
        // =====================================================================
        public static async Task ResetAndSeedAsync(AppDbContext context)
        {
            // Sayaçları sıfırla
            _macCounter = 100;
            _serialCounter = 98234100;

            // 1) TEMİZLİK — FK bağımlılık sırasına göre (çocuktan ebeveyne)
            context.Connections.RemoveRange(context.Connections);
            context.Credentials.RemoveRange(context.Credentials);
            context.Ports.RemoveRange(context.Ports);
            context.Devices.RemoveRange(context.Devices);
            context.Rooms.RemoveRange(context.Rooms);
            context.Floors.RemoveRange(context.Floors);
            context.Facilities.RemoveRange(context.Facilities);
            await context.SaveChangesAsync();

            // 2) YENİDEN İNŞA — 9 bina, aynı kalitede
            var allFacilities = new List<(Facility Facility, List<Connection> Connections)>
            {
                BuildMerkezBina(),
                BuildEskiBelediyeBinasi(),
                BuildKulturMerkeziVe724Kutuphane(),
                BuildSemtKutuphanesi("Altıntepe Semt Kütüphanesi", "Kiçiköy Mah., Altıntepe, Talas, Kayseri", 4),
                BuildSemtKutuphanesi("Abdülhamidhan Semt Kütüphanesi", "Mevlana Mah., Talas, Kayseri", 5),
                BuildSemtKutuphanesi("Hafize-Ahmet Güldal Semt Kütüphanesi", "Bahçelievler Mah., Talas, Kayseri", 6),
                BuildFenIsleriSantiyesi(),
                BuildAliDagi360(),
                BuildKadinKulturMerkezi()
            };

            foreach (var seed in allFacilities)
            {
                context.Facilities.Add(seed.Facility);
                if (seed.Connections.Count > 0)
                    context.Connections.AddRange(seed.Connections);
            }

            await context.SaveChangesAsync();
        }

        // =====================================================================
        // CİHAZ ÜRETİM YARDIMCILARI (MAC, Seri No ve Çoklu Port Eklendi)
        // =====================================================================
        private static Device BuildRouter(string name, string model, string ip)
        {
            var d = new Device 
            { 
                Name = name, 
                DeviceType = "Router", 
                Status = "Active", 
                Model = model,
                MacAddress = GenerateMac(),
                SerialNumber = GenerateSerial()
            };
            
            d.Ports.Add(new Port { PortNumber = "Gi0/0/1", Status = "Connected" });
            for (int i = 2; i <= 24; i++)
                d.Ports.Add(new Port { PortNumber = $"Gi0/0/{i}", Status = "Disconnected" });

            d.Credential = new Credential { IpAddress = ip, Username = "admin", Password = "Talas@2026!" };
            return d;
        }

        private static Device BuildFirewall(string name, string model, string ip)
        {
            var d = new Device 
            { 
                Name = name, 
                DeviceType = "Firewall", 
                Status = "Active", 
                Model = model,
                MacAddress = GenerateMac(),
                SerialNumber = GenerateSerial()
            };

            d.Ports.Add(new Port { PortNumber = "WAN1", Status = "Connected" });
            d.Ports.Add(new Port { PortNumber = "LAN1", Status = "Connected" });
            for (int i = 2; i <= 24; i++)
                d.Ports.Add(new Port { PortNumber = $"LAN{i}", Status = "Disconnected" });

            d.Credential = new Credential { IpAddress = ip, Username = "admin", Password = "Talas@2026!" };
            return d;
        }

        private static Device BuildSwitch(string name, string model, string ip, int portCount = 24)
        {
            var d = new Device 
            { 
                Name = name, 
                DeviceType = "Switch", 
                Status = "Active", 
                Model = model,
                MacAddress = GenerateMac(),
                SerialNumber = GenerateSerial()
            };

            for (int i = 1; i <= portCount; i++)
                d.Ports.Add(new Port { PortNumber = $"Gi0/{i}", Status = i <= 6 ? "Connected" : "Disconnected" });

            d.Credential = new Credential { IpAddress = ip, Username = "admin", Password = "Talas@2026!" };
            return d;
        }

        private static Device BuildAp(string name, string model, string ip)
        {
            var d = new Device 
            { 
                Name = name, 
                DeviceType = "Access Point", 
                Status = "Active", 
                Model = model,
                MacAddress = GenerateMac(),
                SerialNumber = GenerateSerial()
            };

            d.Ports.Add(new Port { PortNumber = "LAN0", Status = "Connected" });
            d.Ports.Add(new Port { PortNumber = "LAN1", Status = "Disconnected" });

            d.Credential = new Credential { IpAddress = ip, Username = "admin", Password = "Talas@2026!" };
            return d;
        }

        private static Device BuildServer(string name, string model, string ip)
        {
            var d = new Device 
            { 
                Name = name, 
                DeviceType = "Server", 
                Status = "Active", 
                Model = model,
                MacAddress = GenerateMac(),
                SerialNumber = GenerateSerial()
            };

            d.Ports.Add(new Port { PortNumber = "NIC1", Status = "Connected" });
            d.Ports.Add(new Port { PortNumber = "NIC2", Status = "Connected" });
            d.Ports.Add(new Port { PortNumber = "NIC3", Status = "Disconnected" });
            d.Ports.Add(new Port { PortNumber = "NIC4", Status = "Disconnected" });

            d.Credential = new Credential { IpAddress = ip, Username = "sysadmin", Password = "Talas@2026!" };
            return d;
        }

        private static Device BuildPrinter(string name, string model, string ip)
        {
            var d = new Device 
            { 
                Name = name, 
                DeviceType = "Printer", 
                Status = "Active", 
                Model = model,
                MacAddress = GenerateMac(),
                SerialNumber = GenerateSerial()
            };

            d.Ports.Add(new Port { PortNumber = "Eth0", Status = "Connected" });

            d.Credential = new Credential { IpAddress = ip, Username = "admin", Password = "Talas@2026!" };
            return d;
        }

        private static Device BuildPc(string name, string model, string ip)
        {
            var d = new Device 
            { 
                Name = name, 
                DeviceType = "PC", 
                Status = "Active", 
                Model = model,
                MacAddress = GenerateMac(),
                SerialNumber = GenerateSerial()
            };

            d.Ports.Add(new Port { PortNumber = "Eth0", Status = "Connected" });

            d.Credential = new Credential { IpAddress = ip, Username = "personel", Password = "Talas@2026!" };
            return d;
        }

        private static List<Device> BuildPcs(string namePrefix, string model, string ipBase, int startHost, int count)
        {
            var list = new List<Device>();
            for (int i = 0; i < count; i++)
                list.Add(BuildPc($"{namePrefix}-PC-{i + 1:D2}", model, $"{ipBase}.{startHost + i}"));
            return list;
        }

        // =====================================================================
        // FACILITY 1: Talas Belediyesi Merkez Binası
        // =====================================================================
        private static (Facility, List<Connection>) BuildMerkezBina()
        {
            var router = BuildRouter("TALAS-CORE-ROUTER", "Cisco ISR 4451", "192.168.1.1");
            var coreSwitch = BuildSwitch("TALAS-CORE-SWITCH-01", "Cisco Catalyst 9300 48-Port", "192.168.1.10", 48);
            var firewall = BuildFirewall("TALAS-FIREWALL-MAIN", "Fortinet FortiGate 100F", "192.168.1.2");
            var server = BuildServer("TALAS-EBELEDIYE-SRV-01", "Dell PowerEdge R440", "192.168.1.5");

            var danismaPcs = BuildPcs("TALAS-MERKEZ-DANISMA", "Dell OptiPlex 5090", "192.168.1", 30, 2);
            var danismaPrinter = BuildPrinter("TALAS-MERKEZ-PRINTER-01", "HP LaserJet M404dn", "192.168.1.40");
            var apGiris = BuildAp("TALAS-MERKEZ-AP-GIRIS", "Ubiquiti UniFi AP AC Pro", "192.168.1.20");

            var veznePcs = BuildPcs("TALAS-MERKEZ-VEZNE", "Dell OptiPlex 5090", "192.168.1", 32, 3);
            var veznePrinter = BuildPrinter("TALAS-MERKEZ-PRINTER-02", "HP LaserJet Pro M130fw", "192.168.1.41");

            var sistemOdasi = new Room { Name = "Ana Bilgi İşlem & Sistem Odası" };
            sistemOdasi.Devices.Add(router);
            sistemOdasi.Devices.Add(coreSwitch);
            sistemOdasi.Devices.Add(firewall);
            sistemOdasi.Devices.Add(server);

            var girisDanisma = new Room { Name = "Giriş / Danışma" };
            foreach (var pc in danismaPcs) girisDanisma.Devices.Add(pc);
            girisDanisma.Devices.Add(danismaPrinter);
            girisDanisma.Devices.Add(apGiris);

            var vezneTahsilat = new Room { Name = "Vezne / Tahsilat Servisi" };
            foreach (var pc in veznePcs) vezneTahsilat.Devices.Add(pc);
            vezneTahsilat.Devices.Add(veznePrinter);

            var zeminKat = new Floor { Name = "Zemin Kat", Level = 0 };
            zeminKat.Rooms.Add(sistemOdasi);
            zeminKat.Rooms.Add(girisDanisma);
            zeminKat.Rooms.Add(vezneTahsilat);

            var kat1Switch = BuildSwitch("TALAS-MERKEZ-1KAT-SWITCH", "Cisco Catalyst 2960-24", "192.168.1.11");
            var yaziIsleriPcs = BuildPcs("TALAS-MERKEZ-YAZIISLERI", "Dell OptiPlex 3090", "192.168.1", 50, 3);
            var yaziIsleriPrinter = BuildPrinter("TALAS-MERKEZ-PRINTER-03", "HP LaserJet Pro M130fw", "192.168.1.42");
            var maliHizmetlerPcs = BuildPcs("TALAS-MERKEZ-MALIHIZMETLER", "Dell OptiPlex 3090", "192.168.1", 53, 3);
            var maliHizmetlerPrinter = BuildPrinter("TALAS-MERKEZ-PRINTER-04", "HP LaserJet Pro M130fw", "192.168.1.43");

            var yaziIsleriOfisi = new Room { Name = "Yazı İşleri Müdürlüğü" };
            foreach (var pc in yaziIsleriPcs) yaziIsleriOfisi.Devices.Add(pc);
            yaziIsleriOfisi.Devices.Add(yaziIsleriPrinter);

            var maliHizmetlerOfisi = new Room { Name = "Mali Hizmetler Müdürlüğü" };
            foreach (var pc in maliHizmetlerPcs) maliHizmetlerOfisi.Devices.Add(pc);
            maliHizmetlerOfisi.Devices.Add(maliHizmetlerPrinter);
            maliHizmetlerOfisi.Devices.Add(kat1Switch);

            var birinciKat = new Floor { Name = "1. Kat", Level = 1 };
            birinciKat.Rooms.Add(yaziIsleriOfisi);
            birinciKat.Rooms.Add(maliHizmetlerOfisi);

            var kat2Switch = BuildSwitch("TALAS-MERKEZ-2KAT-SWITCH", "Cisco Catalyst 2960-24", "192.168.1.12");
            var baskanlikPc = BuildPc("TALAS-MERKEZ-BASKANLIK-PC-01", "Dell OptiPlex 7090", "192.168.1.60");
            var baskanlikAp = BuildAp("TALAS-MERKEZ-AP-BASKANLIK", "Ubiquiti UniFi AP AC Pro", "192.168.1.21");
            var basinYayinPcs = BuildPcs("TALAS-MERKEZ-BASINYAYIN", "Dell OptiPlex 5090", "192.168.1", 61, 2);

            var baskanlikMakami = new Room { Name = "Başkanlık Makamı Sistem Kabini" };
            baskanlikMakami.Devices.Add(kat2Switch);
            baskanlikMakami.Devices.Add(baskanlikPc);
            baskanlikMakami.Devices.Add(baskanlikAp);

            var basinYayinOfisi = new Room { Name = "Basın Yayın ve Halkla İlişkiler" };
            foreach (var pc in basinYayinPcs) basinYayinOfisi.Devices.Add(pc);

            var ikinciKat = new Floor { Name = "1. Kat (Başkanlık)", Level = 2 };
            ikinciKat.Rooms.Add(baskanlikMakami);
            ikinciKat.Rooms.Add(basinYayinOfisi);

            var meclisAp = BuildAp("TALAS-MERKEZ-AP-MECLIS", "Ubiquiti UniFi AP AC Pro", "192.168.1.22");
            var meclisSunumPc = BuildPc("TALAS-MERKEZ-MECLIS-SUNUM-PC-01", "Lenovo ThinkCentre M70q", "192.168.1.62");
            var encumenAp = BuildAp("TALAS-MERKEZ-AP-ENCUMEN", "Ubiquiti UniFi AP AC Lite", "192.168.1.23");

            var meclisSalonu = new Room { Name = "Belediye Meclis Salonu" };
            meclisSalonu.Devices.Add(meclisAp);
            meclisSalonu.Devices.Add(meclisSunumPc);

            var encumenSalonu = new Room { Name = "Encümen Salonu" };
            encumenSalonu.Devices.Add(encumenAp);

            var ucuncuKat = new Floor { Name = "2. Kat (Meclis)", Level = 3 };
            ucuncuKat.Rooms.Add(meclisSalonu);
            ucuncuKat.Rooms.Add(encumenSalonu);

            var facility = new Facility
            {
                Name = "Talas Belediyesi Merkez Binası",
                FacilityType = "Merkez Hizmet Binası",
                Address = "Yenidoğan Mah., Pazar Cad. No:10, 38280 Talas, Kayseri"
            };
            facility.Floors.Add(zeminKat);
            facility.Floors.Add(birinciKat);
            facility.Floors.Add(ikinciKat);
            facility.Floors.Add(ucuncuKat);

            var connections = new List<Connection>
            {
                new Connection { ConnectionType = "Fiber Optic", SourcePort = router.Ports.First(), TargetDevice = coreSwitch },
                new Connection { ConnectionType = "Fiber", SourcePort = coreSwitch.Ports.First(p => p.PortNumber == "Gi0/1"), TargetDevice = firewall },
                new Connection { ConnectionType = "Cat6", SourcePort = coreSwitch.Ports.First(p => p.PortNumber == "Gi0/2"), TargetDevice = server },
                new Connection { ConnectionType = "Cat6", SourcePort = coreSwitch.Ports.First(p => p.PortNumber == "Gi0/3"), TargetDevice = apGiris },
                new Connection { ConnectionType = "Fiber", SourcePort = coreSwitch.Ports.First(p => p.PortNumber == "Gi0/4"), TargetDevice = kat1Switch },
                new Connection { ConnectionType = "Fiber", SourcePort = coreSwitch.Ports.First(p => p.PortNumber == "Gi0/5"), TargetDevice = kat2Switch },
                new Connection { ConnectionType = "Cat6", SourcePort = kat1Switch.Ports.First(p => p.PortNumber == "Gi0/1"), TargetDevice = yaziIsleriPcs[0] },
                new Connection { ConnectionType = "Cat6", SourcePort = kat2Switch.Ports.First(p => p.PortNumber == "Gi0/1"), TargetDevice = baskanlikAp }
            };

            return (facility, connections);
        }

        // =====================================================================
        // FACILITY 2: Eski Belediye Binası
        // =====================================================================
        private static (Facility, List<Connection>) BuildEskiBelediyeBinasi()
        {
            var router = BuildRouter("TALAS-ESKIBLD-ROUTER", "TP-Link ER605", "192.168.2.1");
            var sw = BuildSwitch("TALAS-ESKIBLD-SWITCH-01", "Cisco Catalyst 2960-24", "192.168.2.10");
            var apNikah = BuildAp("TALAS-ESKIBLD-AP-01", "Ubiquiti UniFi AP AC Lite", "192.168.2.20");
            var apToplanti = BuildAp("TALAS-ESKIBLD-AP-02", "Ubiquiti UniFi AP AC Lite", "192.168.2.21");
            var pcNikah = BuildPc("TALAS-ESKIBLD-NIKAH-PC-01", "Dell OptiPlex 3080", "192.168.2.30");
            var pcDanisma = BuildPc("TALAS-ESKIBLD-DANISMA-PC-01", "Dell OptiPlex 3080", "192.168.2.31");
            var printer = BuildPrinter("TALAS-ESKIBLD-PRINTER-01", "HP LaserJet Pro M15w", "192.168.2.40");

            var teknikDolap = new Room { Name = "Teknik Dolap" };
            teknikDolap.Devices.Add(router);
            teknikDolap.Devices.Add(sw);

            var nikahSalonu = new Room { Name = "Nikah Salonu" };
            nikahSalonu.Devices.Add(apNikah);
            nikahSalonu.Devices.Add(pcNikah);

            var danisma = new Room { Name = "Giriş / Danışma" };
            danisma.Devices.Add(pcDanisma);
            danisma.Devices.Add(printer);

            var zeminKat = new Floor { Name = "Zemin Kat", Level = 0 };
            zeminKat.Rooms.Add(teknikDolap);
            zeminKat.Rooms.Add(nikahSalonu);
            zeminKat.Rooms.Add(danisma);

            var arsiv = new Room { Name = "Arşiv Odası" };
            var toplanti = new Room { Name = "Toplantı Odası" };
            toplanti.Devices.Add(apToplanti);

            var birinciKat = new Floor { Name = "1. Kat", Level = 1 };
            birinciKat.Rooms.Add(arsiv);
            birinciKat.Rooms.Add(toplanti);

            var facility = new Facility
            {
                Name = "Eski Belediye Binası",
                FacilityType = "Tarihi Bina / Nikah Salonu",
                Address = "Tablakaya Mah., Cumhuriyet Meydanı, Talas, Kayseri"
            };
            facility.Floors.Add(zeminKat);
            facility.Floors.Add(birinciKat);

            var connections = new List<Connection>
            {
                new Connection { ConnectionType = "Fiber", SourcePort = router.Ports.First(), TargetDevice = sw },
                new Connection { ConnectionType = "Cat6", SourcePort = sw.Ports.First(p => p.PortNumber == "Gi0/1"), TargetDevice = apNikah },
                new Connection { ConnectionType = "Cat6", SourcePort = sw.Ports.First(p => p.PortNumber == "Gi0/2"), TargetDevice = apToplanti }
            };

            return (facility, connections);
        }

        // =====================================================================
        // FACILITY 3: Kültür Merkezi ve 7/24 Kütüphane
        // =====================================================================
        private static (Facility, List<Connection>) BuildKulturMerkeziVe724Kutuphane()
        {
            var router = BuildRouter("TALAS-KULTUR-ROUTER", "MikroTik RB4011", "192.168.3.1");
            var firewall = BuildFirewall("TALAS-KULTUR-FIREWALL", "Fortinet FortiGate 60F", "192.168.3.2");
            var coreSwitch = BuildSwitch("TALAS-KULTUR-CORE-SWITCH", "Cisco Catalyst 9200-24", "192.168.3.10");
            var server = BuildServer("TALAS-KULTUR-OTOMASYON-SRV", "Dell PowerEdge R340", "192.168.3.5");

            var pcDanisma = BuildPcs("TALAS-KULTUR-DANISMA", "Dell OptiPlex 5090", "192.168.3", 30, 2);
            var printerDanisma = BuildPrinter("TALAS-KULTUR-PRINTER-01", "HP LaserJet M404dn", "192.168.3.40");
            var apGiris = BuildAp("TALAS-KULTUR-AP-GIRIS", "Ubiquiti UniFi AP AC Pro", "192.168.3.20");

            var apSalon1 = BuildAp("TALAS-KULTUR-AP-SALON-01", "Ubiquiti UniFi AP AC Pro", "192.168.3.21");
            var apSalon2 = BuildAp("TALAS-KULTUR-AP-SALON-02", "Ubiquiti UniFi AP AC Pro", "192.168.3.22");
            var kioskPc = BuildPc("TALAS-KULTUR-SELFCHECK-01", "Lenovo ThinkCentre M70q", "192.168.3.31");

            var apCalisma1 = BuildAp("TALAS-KULTUR-AP-CALISMA-01", "Ubiquiti UniFi AP AC Lite", "192.168.3.23");
            var apCalisma2 = BuildAp("TALAS-KULTUR-AP-CALISMA-02", "Ubiquiti UniFi AP AC Lite", "192.168.3.24");

            var personelSwitch = BuildSwitch("TALAS-KULTUR-PERSONEL-SWITCH", "HP Aruba 2530-24", "192.168.3.11");
            var personelPcs = BuildPcs("TALAS-KULTUR-PERSONEL", "Dell OptiPlex 3090", "192.168.3", 50, 3);
            var personelPrinter = BuildPrinter("TALAS-KULTUR-PRINTER-02", "HP LaserJet Pro M130fw", "192.168.3.41");

            var apCokAmacli = BuildAp("TALAS-KULTUR-AP-COKAMACLI", "Ubiquiti UniFi AP AC Pro", "192.168.3.25");
            var sunumPc = BuildPc("TALAS-KULTUR-SUNUM-PC-01", "Dell OptiPlex 3090", "192.168.3.60");
            var apAtolye = BuildAp("TALAS-KULTUR-AP-ATOLYE", "Ubiquiti UniFi AP AC Lite", "192.168.3.26");

            var sistemOdasi = new Room { Name = "Sistem Odası" };
            sistemOdasi.Devices.Add(router);
            sistemOdasi.Devices.Add(firewall);
            sistemOdasi.Devices.Add(coreSwitch);
            sistemOdasi.Devices.Add(server);

            var girisDanisma = new Room { Name = "Giriş / Danışma" };
            foreach (var pc in pcDanisma) girisDanisma.Devices.Add(pc);
            girisDanisma.Devices.Add(printerDanisma);
            girisDanisma.Devices.Add(apGiris);

            var kutuphaneSalonu = new Room { Name = "Halka Açık Kütüphane Salonu" };
            kutuphaneSalonu.Devices.Add(apSalon1);
            kutuphaneSalonu.Devices.Add(apSalon2);
            kutuphaneSalonu.Devices.Add(kioskPc);

            var zeminKat = new Floor { Name = "Zemin Kat", Level = 0 };
            zeminKat.Rooms.Add(sistemOdasi);
            zeminKat.Rooms.Add(girisDanisma);
            zeminKat.Rooms.Add(kutuphaneSalonu);

            var calismaSalonu1 = new Room { Name = "Çalışma Salonu 1" };
            calismaSalonu1.Devices.Add(apCalisma1);

            var calismaSalonu2 = new Room { Name = "Çalışma Salonu 2" };
            calismaSalonu2.Devices.Add(apCalisma2);

            var personelOfisi = new Room { Name = "Personel Ofisi" };
            personelOfisi.Devices.Add(personelSwitch);
            foreach (var pc in personelPcs) personelOfisi.Devices.Add(pc);
            personelOfisi.Devices.Add(personelPrinter);

            var birinciKat = new Floor { Name = "1. Kat", Level = 1 };
            birinciKat.Rooms.Add(calismaSalonu1);
            birinciKat.Rooms.Add(calismaSalonu2);
            birinciKat.Rooms.Add(personelOfisi);

            var cokAmacliSalon = new Room { Name = "Çok Amaçlı Salon (Sinema/Tiyatro)" };
            cokAmacliSalon.Devices.Add(apCokAmacli);
            cokAmacliSalon.Devices.Add(sunumPc);

            var sanatAtolyesi = new Room { Name = "Sanat Atölyesi" };
            sanatAtolyesi.Devices.Add(apAtolye);

            var ikinciKat = new Floor { Name = "2. Kat", Level = 2 };
            ikinciKat.Rooms.Add(cokAmacliSalon);
            ikinciKat.Rooms.Add(sanatAtolyesi);

            var facility = new Facility
            {
                Name = "Talas Bld. Kültür Merkezi ve 7/24 Kütüphane",
                FacilityType = "Kültür Merkezi",
                Address = "Yenidoğan Mah., Şaban Yılmaz Cad. No:1, Talas, Kayseri"
            };
            facility.Floors.Add(zeminKat);
            facility.Floors.Add(birinciKat);
            facility.Floors.Add(ikinciKat);

            var connections = new List<Connection>
            {
                new Connection { ConnectionType = "Fiber", SourcePort = router.Ports.First(), TargetDevice = firewall },
                new Connection { ConnectionType = "Fiber", SourcePort = firewall.Ports.First(p => p.PortNumber == "LAN1"), TargetDevice = coreSwitch },
                new Connection { ConnectionType = "Cat6", SourcePort = coreSwitch.Ports.First(p => p.PortNumber == "Gi0/1"), TargetDevice = server },
                new Connection { ConnectionType = "Cat6", SourcePort = coreSwitch.Ports.First(p => p.PortNumber == "Gi0/2"), TargetDevice = apGiris },
                new Connection { ConnectionType = "Fiber", SourcePort = coreSwitch.Ports.First(p => p.PortNumber == "Gi0/3"), TargetDevice = personelSwitch },
                new Connection { ConnectionType = "Cat6", SourcePort = personelSwitch.Ports.First(p => p.PortNumber == "Gi0/1"), TargetDevice = apCalisma1 },
                new Connection { ConnectionType = "Cat6", SourcePort = personelSwitch.Ports.First(p => p.PortNumber == "Gi0/2"), TargetDevice = apCalisma2 }
            };

            return (facility, connections);
        }

        // =====================================================================
        // FACILITY 4/5/6: Semt Kütüphaneleri
        // =====================================================================
        private static (Facility, List<Connection>) BuildSemtKutuphanesi(string name, string address, int ipOctet)
        {
            var sw = BuildSwitch($"TALAS-SEMT{ipOctet}-SWITCH-01", "TP-Link TL-SG1016D", $"192.168.{ipOctet}.10");
            var ap = BuildAp($"TALAS-SEMT{ipOctet}-AP-01", "Ubiquiti UniFi AP AC Lite", $"192.168.{ipOctet}.20");
            var pc = BuildPc($"TALAS-SEMT{ipOctet}-UYE-PC-01", "Dell OptiPlex 3070", $"192.168.{ipOctet}.30");

            var teknikDolap = new Room { Name = "Teknik Dolap" };
            teknikDolap.Devices.Add(sw);

            var okumaSalonu = new Room { Name = "Okuma Salonu" };
            okumaSalonu.Devices.Add(ap);
            okumaSalonu.Devices.Add(pc);

            var zeminKat = new Floor { Name = "Zemin Kat", Level = 0 };
            zeminKat.Rooms.Add(teknikDolap);
            zeminKat.Rooms.Add(okumaSalonu);

            var facility = new Facility { Name = name, FacilityType = "Semt Kütüphanesi", Address = address };
            facility.Floors.Add(zeminKat);

            var connections = new List<Connection>
            {
                new Connection { ConnectionType = "Cat6", SourcePort = sw.Ports.First(p => p.PortNumber == "Gi0/1"), TargetDevice = ap },
                new Connection { ConnectionType = "Cat6", SourcePort = sw.Ports.First(p => p.PortNumber == "Gi0/2"), TargetDevice = pc }
            };

            return (facility, connections);
        }

        // =====================================================================
        // FACILITY 7: Fen İşleri Şantiyesi
        // =====================================================================
        private static (Facility, List<Connection>) BuildFenIsleriSantiyesi()
        {
            var sw = BuildSwitch("TALAS-FENISLERI-SWITCH-01", "TP-Link TL-SG1016D", "192.168.7.10");
            var pcs = BuildPcs("TALAS-FENISLERI-IDARI", "Dell OptiPlex 3070", "192.168.7", 30, 2);
            var printer = BuildPrinter("TALAS-FENISLERI-PRINTER-01", "HP LaserJet Pro M15w", "192.168.7.40");
            var pcDepo = BuildPc("TALAS-FENISLERI-DEPO-PC-01", "Dell OptiPlex 3070", "192.168.7.50");

            var idariOfis = new Room { Name = "İdari Ofis" };
            idariOfis.Devices.Add(sw);
            foreach (var pc in pcs) idariOfis.Devices.Add(pc);
            idariOfis.Devices.Add(printer);

            var depoOfisi = new Room { Name = "Malzeme / Depo Ofisi" };
            depoOfisi.Devices.Add(pcDepo);

            var zeminKat = new Floor { Name = "Zemin Kat", Level = 0 };
            zeminKat.Rooms.Add(idariOfis);
            zeminKat.Rooms.Add(depoOfisi);

            var facility = new Facility
            {
                Name = "Talas Belediyesi Fen İşleri Şantiyesi",
                FacilityType = "Teknik Hizmet / Şantiye Binası",
                Address = "Yenidoğan Mah., Ali Dağı Yolu Üzeri, Talas, Kayseri"
            };
            facility.Floors.Add(zeminKat);

            var connections = new List<Connection>
            {
                new Connection { ConnectionType = "Cat6", SourcePort = sw.Ports.First(p => p.PortNumber == "Gi0/1"), TargetDevice = pcs[0] },
                new Connection { ConnectionType = "Cat6", SourcePort = sw.Ports.First(p => p.PortNumber == "Gi0/2"), TargetDevice = pcs[1] }
            };

            return (facility, connections);
        }

        // =====================================================================
        // FACILITY 8: Ali Dağı 360 Tesisleri
        // =====================================================================
        private static (Facility, List<Connection>) BuildAliDagi360()
        {
            var router = BuildRouter("TALAS-ALIDAGI-ROUTER", "TP-Link ER605", "192.168.8.1");
            var sw = BuildSwitch("TALAS-ALIDAGI-SWITCH-01", "Cisco Catalyst 2960-24", "192.168.8.10");
            var ap1 = BuildAp("TALAS-ALIDAGI-AP-01", "Ubiquiti UniFi AP AC Mesh", "192.168.8.20");
            var ap2 = BuildAp("TALAS-ALIDAGI-AP-02", "Ubiquiti UniFi AP AC Mesh", "192.168.8.21");
            var posPc = BuildPc("TALAS-ALIDAGI-KASA-PC-01", "Lenovo ThinkCentre M70q", "192.168.8.30");
            var ofisPc = BuildPc("TALAS-ALIDAGI-YONETIM-PC-01", "Dell OptiPlex 3070", "192.168.8.31");
            var printer = BuildPrinter("TALAS-ALIDAGI-PRINTER-01", "HP LaserJet Pro M15w", "192.168.8.40");

            var teknikDolap = new Room { Name = "Teknik Dolap" };
            teknikDolap.Devices.Add(router);
            teknikDolap.Devices.Add(sw);

            var kafeterya = new Room { Name = "Kafeterya / Sosyal Alan" };
            kafeterya.Devices.Add(ap1);
            kafeterya.Devices.Add(ap2);
            kafeterya.Devices.Add(posPc);

            var yonetimOfisi = new Room { Name = "Yönetim Ofisi" };
            yonetimOfisi.Devices.Add(ofisPc);
            yonetimOfisi.Devices.Add(printer);

            var zeminKat = new Floor { Name = "Zemin Kat", Level = 0 };
            zeminKat.Rooms.Add(teknikDolap);
            zeminKat.Rooms.Add(kafeterya);
            zeminKat.Rooms.Add(yonetimOfisi);

            var facility = new Facility
            {
                Name = "Ali Dağı 360 Tesisleri",
                FacilityType = "Sosyal Tesis",
                Address = "Han Mah., Ali Dağı Mevkii, Talas, Kayseri"
            };
            facility.Floors.Add(zeminKat);

            var connections = new List<Connection>
            {
                new Connection { ConnectionType = "Fiber", SourcePort = router.Ports.First(), TargetDevice = sw },
                new Connection { ConnectionType = "Wireless", SourcePort = sw.Ports.First(p => p.PortNumber == "Gi0/1"), TargetDevice = ap1 },
                new Connection { ConnectionType = "Wireless", SourcePort = sw.Ports.First(p => p.PortNumber == "Gi0/2"), TargetDevice = ap2 },
                new Connection { ConnectionType = "Cat6", SourcePort = sw.Ports.First(p => p.PortNumber == "Gi0/3"), TargetDevice = posPc }
            };

            return (facility, connections);
        }

        // =====================================================================
        // FACILITY 9: Kadın Kültür ve Sosyal Tesisi
        // =====================================================================
        private static (Facility, List<Connection>) BuildKadinKulturMerkezi()
        {
            var router = BuildRouter("TALAS-KADINKULTUR-ROUTER", "TP-Link ER605", "192.168.9.1");
            var sw = BuildSwitch("TALAS-KADINKULTUR-SWITCH-01", "Cisco Catalyst 2960-24", "192.168.9.10");
            var danismaPc = BuildPc("TALAS-KADINKULTUR-DANISMA-PC-01", "Dell OptiPlex 3070", "192.168.9.30");
            var printer = BuildPrinter("TALAS-KADINKULTUR-PRINTER-01", "HP LaserJet Pro M130fw", "192.168.9.40");
            var apKurs1 = BuildAp("TALAS-KADINKULTUR-AP-01", "Ubiquiti UniFi AP AC Lite", "192.168.9.20");

            var apKurs2 = BuildAp("TALAS-KADINKULTUR-AP-02", "Ubiquiti UniFi AP AC Lite", "192.168.9.21");
            var egitmenPc = BuildPc("TALAS-KADINKULTUR-EGITMEN-PC-01", "Dell OptiPlex 3070", "192.168.9.31");
            var apCokAmacli = BuildAp("TALAS-KADINKULTUR-AP-03", "Ubiquiti UniFi AP AC Lite", "192.168.9.22");

            var sistemOdasi = new Room { Name = "Sistem Odası" };
            sistemOdasi.Devices.Add(router);
            sistemOdasi.Devices.Add(sw);

            var danisma = new Room { Name = "Giriş / Danışma" };
            danisma.Devices.Add(danismaPc);
            danisma.Devices.Add(printer);

            var kursSalonu1 = new Room { Name = "Kurs Salonu 1" };
            kursSalonu1.Devices.Add(apKurs1);

            var zeminKat = new Floor { Name = "Zemin Kat", Level = 0 };
            zeminKat.Rooms.Add(sistemOdasi);
            zeminKat.Rooms.Add(danisma);
            zeminKat.Rooms.Add(kursSalonu1);

            var kursSalonu2 = new Room { Name = "Kurs Salonu 2" };
            kursSalonu2.Devices.Add(apKurs2);
            kursSalonu2.Devices.Add(egitmenPc);

            var cokAmacliSalon = new Room { Name = "Çok Amaçlı Salon" };
            cokAmacliSalon.Devices.Add(apCokAmacli);

            var birinciKat = new Floor { Name = "1. Kat", Level = 1 };
            birinciKat.Rooms.Add(kursSalonu2);
            birinciKat.Rooms.Add(cokAmacliSalon);

            var facility = new Facility
            {
                Name = "Talas Kadın Kültür ve Sosyal Tesisi",
                FacilityType = "Kadın Kültür Merkezi",
                Address = "Bahçelievler Mah., Atatürk Bulvarı No:42, Talas, Kayseri"
            };
            facility.Floors.Add(zeminKat);
            facility.Floors.Add(birinciKat);

            var connections = new List<Connection>
            {
                new Connection { ConnectionType = "Fiber", SourcePort = router.Ports.First(), TargetDevice = sw },
                new Connection { ConnectionType = "Cat6", SourcePort = sw.Ports.First(p => p.PortNumber == "Gi0/1"), TargetDevice = apKurs1 },
                new Connection { ConnectionType = "Cat6", SourcePort = sw.Ports.First(p => p.PortNumber == "Gi0/2"), TargetDevice = danismaPc }
            };

            return (facility, connections);
        }
    }
}