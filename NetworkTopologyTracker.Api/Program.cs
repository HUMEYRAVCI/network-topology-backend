using Microsoft.EntityFrameworkCore;
using NetworkTopologyTracker.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Veritabanı Bağlantısını Tanımlama
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// 🔑 ADIM: JWT Kimlik Doğrulama (Authentication) Servislerini Sisteme Tanıtıyoruz
var jwtKey = builder.Configuration["Jwt:Key"] ?? "TalasBelediyesiCokGizliVeGuvenliKeyArayuzuSiberGuvenlik2026!";
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Geliştirme ortamında (localhost) HTTPS zorunluluğunu kaldırıyoruz
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "NetworkTopologyApi",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "NetworkTopologyFrontend",
        ValidateLifetime = true, // Token süresi (1 gün) bittiğinde geçersiz sayılması için
        ClockSkew = TimeSpan.Zero
    };
});

// 2. CORS Ayarları (Arkadaşının Vue.js arayüzünün backend ile engelsiz konuşabilmesi için TEK ve TEMİZ tanım)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", policy =>
    {
        policy.WithOrigins(
                    "http://localhost:5173", 
                    "http://localhost:5174", 
                    "http://localhost:8080", 
                    "http://127.0.0.1:5173"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "NetworkTopologyTracker.Api", Version = "v1" });
    
    // Swagger'a JWT Bearer (Token) desteğini tanımlıyoruz
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
app.UseMiddleware<NetworkTopologyTracker.Api.Middlewares.ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

// Veritabanını Otomatik Oluşturma ve Sahte Verileri (Seed Data) Basma Motoru
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        
        // Docker'daki SQL Server ayağa kalkarken C# hemen bağlanmaya çalışırsa çökebilir.
        await context.Database.EnsureCreatedAsync();
        
        // Sahte test verilerini veritabanına pompalıyoruz
        await SeedData.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı oluşturulurken veya Seed verileri basılırken bir hata oluştu.");
    }
}

// 🎯 KRİTİK SIRALAMA: Önce CORS izni verilir, sonra kimlik doğrulanır ve yetkilendirilir!
app.UseCors("AllowVueApp"); // 1. Kapı Vue.js isteklerine açılır

app.UseAuthentication();    // 2. Kimlik doğrulaması yapılır
app.UseAuthorization();     // 3. Yetkisi var mı bakılır

app.MapControllers();

app.Run();