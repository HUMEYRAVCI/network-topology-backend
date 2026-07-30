using Microsoft.EntityFrameworkCore;
using NetworkTopologyTracker.Api.Entities;

namespace NetworkTopologyTracker.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

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

            // ==================== HİYERARŞİ İLİŞKİLERİ ====================
            // NOT: Bu sınıf artık HİÇBİR seed verisi (HasData) içermiyor.
            // Tüm seed işlemleri SeedData.cs üzerinden, uygulama başlangıcında,
            // runtime'da çalıştırılıyor (özellikle şifre hash'leri için bu zorunlu,
            // çünkü PasswordHasher her çağrıda farklı bir salt üretir ve bu salt
            // migration dosyasına elle/statik olarak yazılamaz).

            modelBuilder.Entity<Floor>()
                .HasOne(f => f.Facility)
                .WithMany(fac => fac.Floors)
                .HasForeignKey(f => f.FacilityId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Room>()
                .HasOne(r => r.Floor)
                .WithMany(f => f.Rooms)
                .HasForeignKey(r => r.FloorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Device>()
                .HasOne(d => d.Room)
                .WithMany(r => r.Devices)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Port>()
                .HasOne(p => p.Device)
                .WithMany(d => d.Ports)
                .HasForeignKey(p => p.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Connection>()
                .HasOne(c => c.SourcePort)
                .WithOne(p => p.Connection)
                .HasForeignKey<Connection>(c => c.SourcePortId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Connection>()
                .HasOne(c => c.TargetDevice)
                .WithMany(d => d.IncomingConnections)
                .HasForeignKey(c => c.TargetDeviceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Device>()
                .HasOne(d => d.Credential)
                .WithOne(c => c.Device)
                .HasForeignKey<Credential>(c => c.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            // ==================== KULLANICI ADI BENZERSİZLİĞİ ====================
            // Aynı kullanıcı adıyla iki kayıt oluşmasını veritabanı seviyesinde engelle.
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
        }
    }
}