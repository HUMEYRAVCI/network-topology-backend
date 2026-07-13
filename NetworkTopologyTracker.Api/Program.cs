using Microsoft.EntityFrameworkCore;
using NetworkTopologyTracker.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Veritabanı Bağlantısını Tanımlama
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. CORS Ayarları (Arkadaşının Vue.js arayüzünün backend ile engelsiz konuşabilmesi için)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Veritabanını Otomatik Oluşturma ve Sahte Verileri (Seed Data) Basma Motoru
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        
        // Docker'daki SQL Server ayağa kalkarken C# hemen bağlanmaya çalışırsa çökebilir.
        // Bu komut veritabanı hazır olana kadar bekler ve otomatik olarak oluşturur.
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

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();