using Microsoft.EntityFrameworkCore;
using NetworkTopologyTracker.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

// =============================================================================
// UYGULAMA BAŞLATICI VE BAĞIMLILIK ENJEKSİYONU (DEPENDENCY INJECTION) YAPILANDIRMASI
// =============================================================================
// Bu dosya (.NET 8 Minimal Hosting Model), uygulamanın servis kayıtlarını (DI Container),
// kimlik doğrulama/yetkilendirme kurallarını, veritabanı otomatik göç (Migration) ve
// tohumlama (Seed) süreçlerini ve HTTP istek işleme hattını (Middleware Pipeline) yapılandırır.
var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------------
// 1. VERİTABANI BAĞLANTISI (ENTITY FRAMEWORK CORE - MS SQL SERVER)
// -----------------------------------------------------------------------------
// appsettings.json veya Docker ortam değişkenlerinden (Environment Variables) gelen
// "DefaultConnection" bağlantı cümlesi okunarak AppDbContext servisi Scoped olarak kaydedilir.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// -----------------------------------------------------------------------------
// 2. JWT (JSON WEB TOKEN) KİMLİK DOĞRULAMA SERVİSLERİ
// -----------------------------------------------------------------------------
// API uç noktalarını korumak için simetrik anahtar (HMAC-SHA256) tabanlı Bearer Token
// doğrulama mekanizması yapılandırılır. Yapılandırma dosyasında anahtar bulunamazsa
// geliştirme ortamı için varsayılan yedek (fallback) anahtar kullanılır.
var jwtKey = builder.Configuration["Jwt:Key"] ?? "TalasBelediyesiCokGizliVeGuvenliKeyArayuzuSiberGuvenlik2026!";
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    // Varsayılan kimlik doğrulama ve yetkisiz erişim (401 Challenge) şeması olarak JWT Bearer seçilir.
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Docker ve yerel ağ (HTTP) ortamlarında geliştirme kolaylığı için HTTPS metaveri zorunluluğu kapatılır.
    options.RequireHttpsMetadata = false;
    // Doğrulanan token'ın HttpContext üzerinden okunabilmesi için bellekte saklanması sağlanır.
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Token imzasının sunucunun gizli anahtarıyla (SymmetricSecurityKey) oluşturulup oluşturulmadığını denetler.
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        // Token'ı üreten kaynağın (Issuer) beklenen API sunucusu olup olmadığını doğrular.
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "NetworkTopologyApi",
        // Token'ın hangi istemci/hedef kitle (Audience) için üretildiğini doğrular.
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "NetworkTopologyFrontend",
        // Token süresinin (Expires) dolup dolmadığını kontrol eder.
        ValidateLifetime = true,
        // Sunucu saat farkı toleransını (varsayılan 5 dk) sıfırlayarak süresi dolan token'ı anında geçersiz kılar.
        ClockSkew = TimeSpan.Zero
    };
});

// -----------------------------------------------------------------------------
// 3. DİNAMİK CORS (CROSS-ORIGIN RESOURCE SHARING) POLİTİKASI
// -----------------------------------------------------------------------------
// Frontend uygulamasının (Vue.js vb.) hem aynı makineden (localhost / 127.0.0.1)
// hem de kurum içi yerel ağdaki (LAN - RFC 1918 özel IP blokları: 192.168.x.x, 10.x.x.x, 172.x.x.x)
// istemcilerden API'ye güvenli şekilde istek atabilmesine izin verir.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
                new Uri(origin).Host == "localhost" ||
                new Uri(origin).Host == "127.0.0.1" ||
                new Uri(origin).Host.StartsWith("192.168.") ||
                new Uri(origin).Host.StartsWith("10.") ||
                new Uri(origin).Host.StartsWith("172."))
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// -----------------------------------------------------------------------------
// 4. HIZ SINIRLANDIRMA (RATE LIMITING - BRUTE FORCE & DoS KORUMASI)
// -----------------------------------------------------------------------------
// İstemci IP adresi bazında Sabit Pencere (Fixed Window) algoritması uygulanır.
// Her IP adresi 1 dakikalık zaman diliminde en fazla 100 istek yapabilir.
// Limit aşıldığında HTTP 429 (Too Many Requests) durum kodu döndürülür.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("fixed", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// -----------------------------------------------------------------------------
// 5. SERVİS SAĞLIK KONTROLÜ (HEALTH CHECKS) VE SWAGGER / OPENAPI DOKÜMANTASYONU
// -----------------------------------------------------------------------------
// Konteyner orkestrasyonu (Docker) ve izleme araçları için "/health" uç noktası servisi eklenir.
builder.Services.AddHealthChecks();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger UI üzerinden JWT Bearer Token ile korumalı uç noktaları ([Authorize]) test edebilmek için
// OpenAPI güvenlik şeması ("Authorize" butonu) tanımlanır.
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "NetworkTopologyTracker.Api", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Lütfen kutuya 'Bearer <senin_tokenin>' şeklinde token girin. Örnek: Bearer eyJhbGciOi...",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// -----------------------------------------------------------------------------
// 6. MERKEZİ HATA YÖNETİMİ (GLOBAL EXCEPTION MIDDLEWARE)
// -----------------------------------------------------------------------------
// İstek hattının (Pipeline) en başına yerleştirilerek alt katmanlarda fırlatılan tüm
// yakalanmamış istisnaları (Unhandled Exceptions) yakalar, loglar ve standart JSON formatında döner.
app.UseMiddleware<NetworkTopologyTracker.Api.Middlewares.ExceptionMiddleware>();

// Swagger arayüzü etkinleştirilir.
app.UseSwagger();
app.UseSwaggerUI();

// -----------------------------------------------------------------------------
// 7. OTOMATİK VERİTABANI GÖÇÜ (MIGRATION) VE BAŞLANGIÇ VERİSİ TOHUMLAMA (SEEDING)
// -----------------------------------------------------------------------------
// Uygulama ayağa kalkarken geçici bir Dependency Injection Scope oluşturulur.
// MİMARİ NOT: EnsureCreated() yerine Migrate() kullanılır; böylece __EFMigrationsHistory tablosu
// üzerinden bekleyen şema değişiklikleri veritabanına güvenli ve kontrollü şekilde uygulanır.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();

        // 1) Kullanıcıları tohumla (Idempotent yapıdadır: Users tablosunda kayıt varsa işlem yapmaz).
        await SeedData.Initialize(context);

        // 2) Ağ topolojisi başlangıç verisini (9 tesis, katlar, odalar, cihazlar, portlar ve bağlantılar)
        // yalnızca Facilities tablosu tamamen boşsa veritabanına yükle.
        if (!await context.Facilities.AnyAsync())
        {
            await DbInitializer.ResetAndSeedAsync(context);
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı migrate/seed edilirken bir hata oluştu.");
    }
}

// -----------------------------------------------------------------------------
// 8. HTTP İSTEK İŞLEME HATTI (MIDDLEWARE PIPELINE SIRALAMASI)
// -----------------------------------------------------------------------------
// ASP.NET Core mimarisinde middleware sırası kritiktir:
// Önce CORS izinleri kontrol edilir -> Hız sınırı (Rate Limiter) uygulanır ->
// Kimlik doğrulama (Authentication - Kimsin?) yapılır -> Yetkilendirme (Authorization - Yetkin var mı?) denetlenir.
app.UseCors("AllowVueApp");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Sağlık kontrolü uç noktası (/health) ve Controller rotaları haritalanır.
app.MapHealthChecks("/health");

app.MapControllers();

app.Run();
