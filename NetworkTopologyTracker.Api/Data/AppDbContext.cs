using Microsoft.EntityFrameworkCore;
using NetworkTopologyTracker.Api.Entities;

namespace NetworkTopologyTracker.Api.Data
{
    /// <summary>
    /// Uygulamanın Veritabanı Bağlam Sınıfı (Entity Framework Core DbContext).
    /// C# varlık sınıfları (Entities) ile ilişkisel veritabanı (MS SQL Server) tabloları arasındaki
    /// ORM (Object-Relational Mapping) eşleştirmesini, yabancı anahtar (Foreign Key) ilişkilerini,
    /// silme davranışlarını (Cascade / Restrict) ve benzersizlik indekslerini yönetir.
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Program.cs içerisindeki Dependency Injection (DI) konteynerinden gelen bağlantı ve sağlayıcı
        /// ayarlarını (DbContextOptions) temel DbContext sınıfına iletir.
        /// </summary>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // =====================================================================
        // VERİTABANI TABLO KÜMELERİ (DBSETS)
        // =====================================================================

        /// <summary>Kuruma ait fiziksel tesis ve binaların tutulduğu tablo.</summary>
        public DbSet<Facility> Facilities { get; set; }

        /// <summary>Binaların içerisindeki katların tutulduğu tablo.</summary>
        public DbSet<Floor> Floors { get; set; }

        /// <summary>Katlarda yer alan oda ve birimlerin tutulduğu tablo.</summary>
        public DbSet<Room> Rooms { get; set; }

        /// <summary>Odalarda konumlandırılmış ağ ve uç cihazların (Router, Switch, AP, PC vb.) tablosu.</summary>
        public DbSet<Device> Devices { get; set; }

        /// <summary>Cihazların üzerindeki fiziksel ağ portlarının tutulduğu tablo.</summary>
        public DbSet<Port> Ports { get; set; }

        /// <summary>Kaynak port ile hedef cihaz arasındaki fiziksel/kablosuz bağlantıların tablosu.</summary>
        public DbSet<Connection> Connections { get; set; }

        /// <summary>Cihazların yönetim arayüzü erişim bilgilerinin (IP, Kullanıcı Adı, Şifre) tutulduğu tablo.</summary>
        public DbSet<Credential> Credentials { get; set; }

        /// <summary>Sisteme giriş yapabilen yetkili ve izleyici kullanıcıların tablosu.</summary>
        public DbSet<User> Users { get; set; }

        /// <summary>Sistemde gerçekleştirilen kritik işlemlerin denetim izi (Audit Log) tablosu.</summary>
        public DbSet<SystemLog> SystemLogs { get; set; }

        /// <summary>
        /// Veritabanı şeması oluşturulurken Fluent API kullanılarak tablolar arası ilişkilerin,
        /// kaskad silme (Cascade Delete) kurallarının ve kısıtlamaların yapılandırıldığı metot.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1) Facility (1) -> Floor (N) İlişkisi:
            // Bir bina silindiğinde ona bağlı tüm katlar da otomatik olarak (Cascade) silinir.
            modelBuilder.Entity<Floor>()
                .HasOne(f => f.Facility)
                .WithMany(fac => fac.Floors)
                .HasForeignKey(f => f.FacilityId)
                .OnDelete(DeleteBehavior.Cascade);

            // 2) Floor (1) -> Room (N) İlişkisi:
            // Bir kat silindiğinde o kattaki tüm odalar otomatik olarak (Cascade) silinir.
            modelBuilder.Entity<Room>()
                .HasOne(r => r.Floor)
                .WithMany(f => f.Rooms)
                .HasForeignKey(r => r.FloorId)
                .OnDelete(DeleteBehavior.Cascade);

            // 3) Room (1) -> Device (N) İlişkisi:
            // Bir oda silindiğinde içindeki tüm ağ cihazları otomatik olarak (Cascade) silinir.
            modelBuilder.Entity<Device>()
                .HasOne(d => d.Room)
                .WithMany(r => r.Devices)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // 4) Device (1) -> Port (N) İlişkisi:
            // Bir cihaz silindiğinde üzerindeki tüm fiziksel portlar otomatik olarak (Cascade) silinir.
            modelBuilder.Entity<Port>()
                .HasOne(p => p.Device)
                .WithMany(d => d.Ports)
                .HasForeignKey(p => p.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            // 5) Port (1) -> Connection (1) İlişkisi (Kaynak Port):
            // MİMARİ NOT: MS SQL Server'da aynı tabloya (Connections) hem Port hem de Device üzerinden
            // birden fazla Cascade Delete yolu gelmesi "Multiple Cascade Paths" hatasına yol açar.
            // Bu nedenle Connection ilişkilerinde DeleteBehavior.Restrict kullanılmış; silme işlemlerinde
            // ilgili bağlantılar TopologyController.FindRelatedConnectionsAsync() ile önce manuel temizlenmiştir.
            modelBuilder.Entity<Connection>()
                .HasOne(c => c.SourcePort)
                .WithOne(p => p.Connection)
                .HasForeignKey<Connection>(c => c.SourcePortId)
                .OnDelete(DeleteBehavior.Restrict);

            // 6) Device (1) -> Connection (N) İlişkisi (Hedef Cihaz):
            // Çoklu kaskad döngüsünü önlemek için hedef cihaz bağlantılarında da Restrict davranışı uygulanır.
            modelBuilder.Entity<Connection>()
                .HasOne(c => c.TargetDevice)
                .WithMany(d => d.IncomingConnections)
                .HasForeignKey(c => c.TargetDeviceId)
                .OnDelete(DeleteBehavior.Restrict);

            // 7) Device (1) -> Credential (1) İlişkisi:
            // Her cihazın en fazla bir kimlik bilgisi kaydı vardır; cihaz silindiğinde kimlik bilgisi de silinir.
            modelBuilder.Entity<Device>()
                .HasOne(d => d.Credential)
                .WithOne(c => c.Device)
                .HasForeignKey<Credential>(c => c.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            // 8) Kullanıcı Adı Tekillik Kısıtlaması (Unique Index):
            // Aynı kullanıcı adıyla (Username) mükerrer kayıt oluşturulmasını veritabanı seviyesinde engeller.
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
        }
    }
}