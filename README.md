# 🌐 Ağ Topoloji Takip Sistemi — Backend API (`NetworkTopologyTracker.Api`)

Bu proje; **Talas Belediyesi Bilgi İşlem Müdürlüğü** bünyesinde dağınık hizmet binalarında ve tesislerde yer alan aktif/pasif ağ donanımlarının (Router, Switch, Firewall, Sunucu, Access Point, PC vb.) merkezi olarak envanterinin tutulmasını, fiziksel yerleşimlerinin mantıksal port bağlantılarıyla eşleştirilmesini ve rol bazlı güvenlik politikalarıyla (**RBAC**) yönetilmesini sağlayan **ASP.NET Core 8.0 Web API** tabanlı arka uç (Backend) servisidir.

---

## 🛠️ Teknoloji Yığını (Tech Stack)

| Katman / Kategori | Teknoloji / Araç | Sürüm / Standart | Kullanım Amacı |
| :--- | :--- | :--- | :--- |
| **Geliştirme Çatısı** | C# & ASP.NET Core Web API | C# 12 / .NET 8.0 | Çekirdek arka uç iş mantığı ve RESTful Web API servisleri |
| **Veri Erişim (ORM)** | Entity Framework Core | EF Core 8.0 | Nesne-ilişkisel eşleme, Code-First veritabanı ve otomatik migrasyon |
| **İlişkisel Veritabanı** | Microsoft SQL Server | MSSQL 2022 Linux | Hiyerarşik ağ topolojisi, kimlik ve denetim günlüğü (audit log) tabloları |
| **Konteynerleştirme** | Docker & Docker Compose | 2 Sunucu Mimarisi | API (`network-api`) ve Veritabanı (`network-db`) konteyner izolasyonu |
| **Kimlik & Güvenlik** | JWT Bearer & PBKDF2 | ASP.NET Identity / HMAC | Belirteç tabanlı oturum, RBAC yetkilendirme, şifre özetleme (Hash) ve Rate Limiting |
| **API Dokümantasyonu** | Swagger UI / OpenAPI | Swashbuckle 6.5 | İnteraktif API dokümantasyonu ve uçtan uca HTTP test arayüzü |

---

## 🏗️ Sistem Mimarisi ve 5 Seviyeli Hiyerarşik Ağ Modeli

Sistem, belediye ağ altyapısını en üst kurum kademesinden tek bir cihazın fiziksel portuna kadar **5 seviyeli ağaç (tree)** yapısında modeller:

```text
1. Kurum / Lokasyon (Talas Belediyesi Genel Yerleşkesi)
 └── 2. Tesis / Hizmet Binası (Facility)  ──► Örn: Ana Hizmet Binası, Merkez Kütüphanesi
      └── 3. Kat ve Oda (Floor & Room)    ──► Örn: Zemin Kat -> Ana Bilgi İşlem ve Sistem Odası
           └── 4. Ağ Cihazı (Device)      ──► Örn: TALAS-CORE-ROUTER, TALAS-CORE-SWITCH-01
                ├── Hassas Kimlik (Credential - 1:1, RBAC Korumalı: IP, Kullanıcı Adı, Şifre)
                └── 5. Fiziksel Port ve Bağlantı (Port & Connection - 1:N / 1:1)
```

### İlişkisel Veri Bütünlüğü Kuralları (`AppDbContext.cs`)
- **Basamaklı Silme (`DeleteBehavior.Cascade`):** `Facility ➔ Floor ➔ Room ➔ Device ➔ Port / Credential` hiyerarşisinde üst birim silindiğinde alt birimler otomatik temizlenerek yetim kayıt (orphan record) oluşumu engellenir.
- **Kısıtlayıcı Silme (`DeleteBehavior.Restrict`):** Fiziksel/mantıksal kablo bağlantılarını temsil eden `Connection` tablosunda SQL Server çoklu basamaklı silme döngülerini (*multiple cascade paths*) önlemek amacıyla kısıtlayıcı silme uygulanmıştır.

---

## 📂 Proje Dizin Yapısı

```text
network_topology/
├── docker-compose.yml                       # 2 Sunuculu (MSSQL + .NET 8 API) Docker Compose orkestrasyonu
├── N_Topology.slnx                          # Çözüm (Solution) dosyası
└── NetworkTopologyTracker.Api/
    ├── Dockerfile                           # Çok aşamalı (Multi-stage) .NET 8 SDK & Runtime imaj tanımı
    ├── Program.cs                           # DI, JWT, PBKDF2, CORS (AllowVueApp), Rate Limiting ve otomatik göç
    ├── Controllers/
    │   ├── AuthController.cs                # Kullanıcı girişi (/api/Auth/login) ve JWT üretimi
    │   ├── TopologyController.cs            # Tesis, kat, oda, cihaz CRUD, arama ve topoloji ağacı servisleri
    │   └── UsersAndLogsController.cs        # Kullanıcı yönetimi (SuperAdmin) ve denetim günlükleri (Audit Log)
    ├── Data/
    │   ├── AppDbContext.cs                  # EF Core DbContext ve Fluent API tablo/ilişki kuralları
    │   ├── DbInitializer.cs                 # Otomatik şema kontrolü ve başlangıç verisi tetikleyicisi
    │   └── SeedData.cs                      # Talas Belediyesi örnek ağ topolojisi ve PBKDF2 kullanıcısı üretimi
    ├── Entities/                            # Veritabanı varlık sınıfları (Facility, Floor, Room, Device, Port,
    │                                        # Connection, Credential, User, SystemLog)
    ├── Middlewares/
    │   └── ExceptionMiddleware.cs           # Merkezi hata yakalama (Global Exception Handling) ara katmanı
    └── Migrations/                          # EF Core Code-First veritabanı göç (Migration) dosyaları
```

---

## 🔐 Güvenlik ve Rol Bazlı Erişim Kontrolü (RBAC)

1. **DTO Seviyesinde Hassas Veri Filtreleme:**
   - **`SuperAdmin` / `Admin`:** Cihaz detay sorgularında (`GET /api/Topology/devices/{id}`) cihazın yönetim IP adresi, kullanıcı adı ve şifresini içeren `securityInfo` nesnesini görüntüleyebilir; bina/cihaz ekleme, güncelleme ve silme işlemlerini yürütebilir.
   - **`Viewer` (Gözlemci):** Yalnızca topoloji ağacını, cihazların genel donanım niteliklerini ve port durumlarını inceleyebilir. `securityInfo` alanı arka uç tarafından `null` olarak filtrelenir ve ağ üzerinden istemciye kesinlikle iletilmez. Yetkisiz yazma veya kullanıcı listeleme taleplerinde sunucu **`HTTP 403 Forbidden`** döndürür.
2. **PBKDF2 Kriptografik Şifre Özetleme:** Kullanıcı parolaları veritabanında düz metin (*plain text*) olarak tutulmaz; `PasswordHasher<User>` (PBKDF2 + kriptografik tuz) ile özetlenerek saklanır.
3. **Kaba Kuvvet (Brute-Force) Koruması:** `/api/Auth/login` uç noktası `FixedWindowRateLimiter` (`LoginPolicy`) ile korunmakta olup dakikada maksimum 5 başarısız denemeye izin verir; aşım durumunda **`HTTP 429 Too Many Requests`** üretir.

---

## 📡 RESTful API Uç Noktaları (Endpoints)

| HTTP Metodu | API Uç Noktası (Endpoint) | Açıklama | Yetki Seviyesi |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/Auth/login` | Kullanıcı adı ve parola doğrulaması yaparak rol bilgisi içeren JWT üretir | Herkese Açık (`LoginPolicy`) |
| `GET` | `/api/Topology/facilities` | Kayıtlı tüm belediye tesis ve hizmet binalarını listeler | Tüm Roller (`Authorize`) |
| `POST` | `/api/Topology/facilities` | Sisteme yeni bir belediye hizmet binası / tesis ekler | `SuperAdmin` |
| `PUT` | `/api/Topology/facilities/{id}` | Mevcut tesisin ad, tip ve adres bilgilerini günceller | `SuperAdmin` |
| `DELETE` | `/api/Topology/facilities/{id}?confirm=true` | Tesisi ve alt birimleri onay parametresiyle sistemden siler | `SuperAdmin` |
| `GET` | `/api/Topology/facilities/{facilityId}/topology` | Seçilen tesise ait kat, oda, cihaz ve bağlantı ağaç yapısını döndürür | Tüm Roller (`Authorize`) |
| `GET` | `/api/Topology/devices/{id}` | Cihaz detaylarını ve portlarını getirir (`securityInfo` yalnızca Admin'e açılır) | Tüm Roller (`Authorize`) |
| `POST` | `/api/Topology/devices` | Seçilen odaya yeni bir ağ cihazı kaydeder | `Admin`, `SuperAdmin` |
| `PUT` | `/api/Topology/devices/{id}` | Mevcut ağ cihazının donanım ve oda bilgilerini günceller | `Admin`, `SuperAdmin` |
| `DELETE` | `/api/Topology/devices/{id}` | Ağ cihazını ve bağlı port/kimlik ilişkilerini sistemden siler | `Admin`, `SuperAdmin` |
| `GET` | `/api/Topology/devices/search?query=...` | Cihaz adı veya fiziksel MAC adresine göre dinamik arama yapar | Tüm Roller (`Authorize`) |
| `GET` | `/api/Topology/logs` | Topoloji üzerinde yapılan son 50 işlem günlüğünü listeler | Tüm Roller (`Authorize`) |
| `GET` | `/api/UsersAndLogs/users` | Sistem kullanıcılarını ve rollerini listeler | `SuperAdmin` (`403 Korumalı`) |
| `GET` | `/api/UsersAndLogs/logs?limit=100` | Sistem genel denetim günlüklerini (`SystemLogs`) listeler | `Admin`, `SuperAdmin` |

---

## 🚀 Kurulum ve Çalıştırma

### Yöntem 1: Docker Compose ile Çift Sunucu Mimarisi (Önerilen)

Uygulama; **1 Veritabanı Sunucusu (`network-db` - MS SQL Server 2022)** ve **1 Uygulama API Sunucusu (`network-api` - .NET 8 Web API)** olmak üzere iki izole konteyner halinde tek komutla ayağa kaldırılır:

```bash
# Proje kök dizininde konteynerleri derleyin ve başlatın
docker compose up -d --build
```

- **Swagger UI (API Test Arayüzü):** `http://localhost:5278/swagger/index.html`
- **MS SQL Server Bağlantısı (DBeaver / SSMS):** `localhost,1433` (Kullanıcı: `sa`, Şifre: `YourStrong@Password123!`, Veritabanı: `NetworkTopologyDb`)

> Uygulama ilk kez ayağa kalktığında `Program.cs` içerisindeki `context.Database.Migrate()` ve `DbInitializer.Initialize()` metotları sayesinde veritabanı tabloları ve Talas Belediyesi örnek ağ topolojisi verileri **otomatik olarak** oluşturulur.

### Yöntem 2: Yerel `.NET 8 SDK` ile Çalıştırma

```bash
cd NetworkTopologyTracker.Api
dotnet restore
dotnet run
```

---

## 🧪 Varsayılan Test Kullanıcıları (Seed Accounts)

Swagger UI üzerinde `/api/Auth/login` uç noktasını test etmek ve üretilen JWT belirtecini sağ üstteki **Authorize (`Bearer <token>`)** penceresine tanımlamak için aşağıdaki hazır hesapları kullanabilirsiniz:

| Rol | Kullanıcı Adı (`username`) | Parola (`password`) | Yetki Kapsamı |
| :--- | :--- | :--- | :--- |
| **SuperAdmin** | `humeyra` | `Password123!` | Tüm CRUD işlemleri, cihaz şifreleri (`securityInfo`), kullanıcı listesi ve loglar |
| **Viewer (Gözlemci)** | `ahmet_viewer` | `Password123!` | Salt-okunur topoloji inceleme (`securityInfo` gizlenir, yazma işlemleri `403` döner) |
