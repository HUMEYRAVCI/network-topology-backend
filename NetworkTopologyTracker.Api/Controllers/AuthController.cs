using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetworkTopologyTracker.Api.Data;
using Microsoft.AspNetCore.Identity;
using NetworkTopologyTracker.Api.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System;

namespace NetworkTopologyTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Giriş İstek Modeli (Vue.js'ten gelecek veri kalıbı)
        public class LoginRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Kullanıcıyı veritabanında ara
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
            {
                // Siber güvenlik gereği kullanıcı bulunamadı demek yerine genel yanıt fırlatıyoruz
                return Unauthorized(new { message = "Kullanıcı adı veya şifre hatalı!" });
            }

            // Şifre kontrolünü veritabanındaki mevcut hash üzerinden güvenli şekilde yap
            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, user.Password, request.Password);

            if (result == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { message = "Kullanıcı adı veya şifre hatalı!" });
            }

            // ==================== GERÇEK ZAMANLI LOG VE SON GİRİŞ GÜNCELLEMESİ ====================
            
            // 1. Kullanıcının son giriş tarihini güncelle
            user.LastLoginAt = DateTime.UtcNow;

            // 2. Sistem logları tablosuna giriş kaydı fırlat
            var log = new SystemLog
            {
                Username = user.Username,
                Action = "Kullanıcı Girişi",
                Details = $"{user.FirstName} {user.LastName} ({user.Role}) sisteme başarıyla giriş yaptı.",
                ActionType = "Giriş",
                Timestamp = DateTime.UtcNow
            };
            await _context.SystemLogs.AddAsync(log);

            // 3. Değişiklikleri veritabanına kalıcı olarak işle
            await _context.SaveChangesAsync();

            // ==================== JWT TOKEN ÜRETİMİ ====================
            var tokenHandler = new JwtSecurityTokenHandler();
            
            var jwtKey = _configuration["Jwt:Key"] ?? "TalasBelediyesiCokGizliVeGuvenliKeyArayuzuSiberGuvenlik2026!";
            var key = Encoding.UTF8.GetBytes(jwtKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("FirstName", user.FirstName ?? string.Empty),
                    new Claim("LastName", user.LastName ?? string.Empty)
                }),
                Expires = DateTime.UtcNow.AddDays(1), // Token 1 gün geçerli
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"] ?? "NetworkTopologyApi",
                Audience = _configuration["Jwt:Audience"] ?? "NetworkTopologyFrontend"
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // Vue.js uygulamasına yanıt dön
            return Ok(new
            {
                user.FirstName,
                user.LastName,
                user.Username,
                user.Role,
                user.LastLoginAt,
                Token = tokenString
            });
        }
    }
}