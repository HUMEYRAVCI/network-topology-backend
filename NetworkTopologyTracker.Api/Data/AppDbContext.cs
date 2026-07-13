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
        public DbSet<Facility> Facilities { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<Port> Ports { get; set; }
        public DbSet<Connection> Connections { get; set; }
        public DbSet<Credential> Credentials { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Hiyerarşi: Room -> Facility (Çoktan Teke)
            modelBuilder.Entity<Room>()
                .HasOne(r => r.Facility)
                .WithMany(f => f.Rooms)
                .HasForeignKey(r => r.FacilityId)
                .OnDelete(DeleteBehavior.Cascade);

            // 2. Hiyerarşi: Device -> Room (Çoktan Teke)
            modelBuilder.Entity<Device>()
                .HasOne(d => d.Room)
                .WithMany(r => r.Devices)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // 3. Ağ Yapısı: Port -> Device (Çoktan Teke)
            modelBuilder.Entity<Port>()
                .HasOne(p => p.Device)
                .WithMany(d => d.Ports)
                .HasForeignKey(p => p.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            // 4. Topoloji Çizimi: Connection -> SourcePort (Bire-Bir Kablo Eşleşmesi)
            modelBuilder.Entity<Connection>()
                .HasOne(c => c.SourcePort)
                .WithOne(p => p.Connection)
                .HasForeignKey<Connection>(c => c.SourcePortId)
                .OnDelete(DeleteBehavior.Restrict);

            // 5. Gizlilik Katmanı: Device <-> Credential (1-1 Hassas Veri İlişkisi)
            // Bir cihazın sadece bir şifre seti olabilir. Cihaz silinirse şifre de silinir.
            modelBuilder.Entity<Device>()
                .HasOne(d => d.Credential)
                .WithOne(c => c.Device)
                .HasForeignKey<Credential>(c => c.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}