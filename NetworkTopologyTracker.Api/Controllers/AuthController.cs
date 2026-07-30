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

        public class LoginRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
            {
                return Unauthorized(new { message = "Kullanıcı adı veya şifre hatalı!" });
            }

            var hasher = new PasswordHasher<User>();
            var verifyResult = hasher.VerifyHashedPassword(user, user.Password, request.Password);

            if (verifyResult == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { message = "Kullanıcı adı veya şifre hatalı!" });
            }

            // ==================== LOG VE SON GİRİŞ GÜNCELLEMESİ (TÜRKİYE SAATİ: UTC+3) ====================
            var turkeyNow = DateTime.UtcNow.AddHours(3);
            user.LastLoginAt = turkeyNow;

            var log = new SystemLog
            {
                Username = user.Username,
                Action = "Kullanıcı Girişi",
                Details = $"{user.FirstName} {user.LastName} ({user.Role}) sisteme başarıyla giriş yaptı.",
                ActionType = "Giriş",
                Timestamp = turkeyNow
            };
            await _context.SystemLogs.AddAsync(log);
            await _context.SaveChangesAsync(); // Veritabanına anında yazılır

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
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"] ?? "NetworkTopologyApi",
                Audience = _configuration["Jwt:Audience"] ?? "NetworkTopologyFrontend"
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new
            {
                user.FirstName,
                user.LastName,
                user.Username,
                user.Role,
                LastLoginAt = user.LastLoginAt.Value.ToString("yyyy-MM-dd HH:mm:ss"), // Türkiye saatli temiz format
                Token = tokenString
            });
        }
    }
}