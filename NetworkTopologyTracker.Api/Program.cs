using Microsoft.EntityFrameworkCore;
using NetworkTopologyTracker.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 1. Veritabanı Bağlantısını Tanımlama
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// 🔑 JWT Kimlik Doğrulama Servisleri
var jwtKey = builder.Configuration["Jwt:Key"] ?? "TalasBelediyesiCokGizliVeGuvenliKeyArayuzuSiberGuvenlik2026!";
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "NetworkTopologyApi",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "NetworkTopologyFrontend",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// 1. ADIM: Dinamik CORS (Localhost + Yerel Ağ IP'leri)
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

// 2. ADIM: Rate Limiting Servisi (Aşırı İstek / DoS Koruması)
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

// 🩺 3. ADIM: Health Check Servisi (Veritabanı Canlılık Kontrolü)
builder.Services.AddHealthChecks();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
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
app.UseMiddleware<NetworkTopologyTracker.Api.Middlewares.ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

// Veritabanını Otomatik Oluşturma ve Seed Data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        await DbInitializer.InitializeAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı oluşturulurken veya Seed verileri basılırken bir hata oluştu.");
    }
}

// Middleware Sıralaması
app.UseCors("AllowVueApp"); 
app.UseRateLimiter();       
app.UseAuthentication();    
app.UseAuthorization();     

// 🩺 3. ADIM: Health Check Uç Noktası Haritalama
app.MapHealthChecks("/health"); 

app.MapControllers();

app.Run();