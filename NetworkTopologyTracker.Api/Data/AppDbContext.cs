using Microsoft.EntityFrameworkCore;
using NetworkTopologyTracker.Api.Entities;

namespace NetworkTopologyTracker.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Modellerimizi SQL tablolarına dönüştürüyoruz
        public DbSet<Institution> Institutions { get; set; }
        public DbSet<Facility> Facilities { get; set; }
        public DbSet<Floor> Floors { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<Port> Ports { get; set; }
        public DbSet<Connection> Connections { get; set; }
        public DbSet<Credential> Credentials { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==================== YENİ HİYERARŞİ İLİŞKİLERİ ====================

            // 1. Facility -> Institution (Binalar Kuruma Bağlanıyor)
            modelBuilder.Entity<Facility>()
                .HasOne(f => f.Institution)
                .WithMany(i => i.Facilities)
                .HasForeignKey(f => f.InstitutionId)
                .OnDelete(DeleteBehavior.Cascade);

            // 2. Floor -> Facility (Katlar Binaya Bağlanıyor)
            modelBuilder.Entity<Floor>()
                .HasOne(f => f.Facility)
                .WithMany(fac => fac.Floors)
                .HasForeignKey(f => f.FacilityId)
                .OnDelete(DeleteBehavior.Cascade);

            // 3. Room -> Floor (Odalar Kata Bağlanıyor)
            modelBuilder.Entity<Room>()
                .HasOne(r => r.Floor)
                .WithMany(f => f.Rooms)
                .HasForeignKey(r => r.FloorId)
                .OnDelete(DeleteBehavior.Cascade);


            // ==================== AĞ VEYA CİHAZ İLİŞKİLERİ ====================

            // 4. Device -> Room (Cihaz Odaya Bağlanıyor)
            modelBuilder.Entity<Device>()
                .HasOne(d => d.Room)
                .WithMany(r => r.Devices)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // 5. Port -> Device (Port Cihaza Bağlanıyor)
            modelBuilder.Entity<Port>()
                .HasOne(p => p.Device)
                .WithMany(d => d.Ports)
                .HasForeignKey(p => p.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            // 6. Topoloji Çizimi: Connection -> SourcePort (Çıkan Kablo)
            modelBuilder.Entity<Connection>()
                .HasOne(c => c.SourcePort)
                .WithOne(p => p.Connection)
                .HasForeignKey<Connection>(c => c.SourcePortId)
                .OnDelete(DeleteBehavior.Restrict);

            // 7. Topoloji Çizimi: Connection -> TargetDevice (Giden Cihaz - EKLLENDİ)
            // SQL Server'da Multiple Cascade Paths hatası oluşmaması için Restrict yapıldı.
            modelBuilder.Entity<Connection>()
                .HasOne(c => c.TargetDevice)
                .WithMany(d => d.IncomingConnections)
                .HasForeignKey(c => c.TargetDeviceId)
                .OnDelete(DeleteBehavior.Restrict);

            // 8. Gizlilik Katmanı: Device <-> Credential (1-1 Hassas Veri)
            modelBuilder.Entity<Device>()
                .HasOne(d => d.Credential)
                .WithOne(c => c.Device)
                .HasForeignKey<Credential>(c => c.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}