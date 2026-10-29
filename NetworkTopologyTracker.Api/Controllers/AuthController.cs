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
    /// <summary>
    /// Kimlik Doğrulama ve Oturum Yönetimi Denetleyicisi (Authentication Controller).
    /// Kullanıcıların sisteme giriş (Login) işlemlerini yürütür, parola özetini (Hash) doğrular,
    /// başarılı girişleri sistem denetim loglarına (<see cref="SystemLog"/>) kaydeder ve
    /// istemciye (Frontend) yetkilendirilmiş isteklerde kullanacağı JWT (JSON Web Token) anahtarını üretir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Veritabanı bağlamını (<see cref="AppDbContext"/>) ve uygulama yapılandırma ayarlarını (<see cref="IConfiguration"/>)
        /// Dependency Injection üzerinden alır.
        /// </summary>
        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// Kullanıcı giriş isteği için kullanılan Veri Transfer Nesnesi (DTO / Request Model).
        /// </summary>
        public class LoginRequest
        {
            /// <summary>Sisteme giriş yapmak isteyen kullanıcının kullanıcı adı.</summary>
            public string Username { get; set; } = string.Empty;
            /// <summary>Kullanıcının girdiği düz metin (plain-text) parola.</summary>
            public string Password { get; set; } = string.Empty;
        }

        /// <summary>
        /// Kullanıcı adı ve şifre doğrulaması yaparak geçerli bir JWT Bearer Token döndürür.
        /// Rota: POST /api/Auth/login
        /// </summary>
        /// <param name="request">Kullanıcı adı ve parola bilgilerini içeren istek gövdesi.</param>
        /// <returns>Başarılıysa kullanıcı profil bilgileri ve JWT Token; başarısızsa HTTP 401 Unauthorized.</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // 1) Gönderilen kullanıcı adına sahip kaydı veritabanında ara.
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            // Güvenlik en iyi pratiği (Security Best Practice): Kullanıcı bulunamadığında, saldırgana
            // hangi alanın yanlış olduğu bilgisini sızdırmamak için genel bir hata mesajı döndürülür.
            if (user == null)
            {
                return Unauthorized(new { message = "Kullanıcı adı veya şifre hatalı!" });
            }

            // 2) Veritabanındaki hash'lenmiş parola ile kullanıcının gönderdiği parolayı karşılaştır.
            var hasher = new PasswordHasher<User>();
            var verifyResult = hasher.VerifyHashedPassword(user, user.Password, request.Password);

            if (verifyResult == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { message = "Kullanıcı adı veya şifre hatalı!" });
            }

            // 3) LOG VE SON GİRİŞ ZAMANI GÜNCELLEMESİ (TÜRKİYE SAATİ: UTC+3)
            // Konteyner veya sunucu UTC saat diliminde çalışabileceği için Türkiye yerel saati (UTC+3) hesaplanır.
            var turkeyNow = DateTime.UtcNow.AddHours(3);
            user.LastLoginAt = turkeyNow;

            // Başarılı oturum açma işlemini denetim izi (Audit Log) tablosuna kaydet.
            var log = new SystemLog
            {
                Username = user.Username,
                Action = "Kullanıcı Girişi",
                Details = $"{user.FirstName} {user.LastName} ({user.Role}) sisteme başarıyla giriş yaptı.",
                ActionType = "Giriş",
                Timestamp = turkeyNow
            };
            await _context.SystemLogs.AddAsync(log);
            await _context.SaveChangesAsync(); // Son giriş tarihi ve log kaydı veritabanına anında işlenir.

            // 4) JWT (JSON WEB TOKEN) ÜRETİMİ
            // Kullanıcının kimlik ve rol bilgilerini (Claims) barındıran, 1 gün geçerli imzalı token oluşturulur.
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtKey = _configuration["Jwt:Key"] ?? "TalasBelediyesiCokGizliVeGuvenliKeyArayuzuSiberGuvenlik2026!";
            var key = Encoding.UTF8.GetBytes(jwtKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                // Token içerisine gömülecek kimlik ve yetki talepleri (Claims):
                // ClaimTypes.Name -> User.Identity.Name üzerinden erişilir (Loglamalarda kullanılır).
                // ClaimTypes.Role -> [Authorize(Roles = "SuperAdmin")] ve User.IsInRole() kontrollerinde kullanılır.
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

            // 5) İstemci arayüzünün (Frontend) ihtiyaç duyduğu kullanıcı bilgileri ve üretilen JWT Token döndürülür.
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