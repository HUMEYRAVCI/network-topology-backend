using System.Net;
using System.Text.Json;

namespace NetworkTopologyTracker.Api.Middlewares
{
    /// <summary>
    /// Merkezi Hata Yakalama Ara Katmanı (Global Exception Handling Middleware).
    /// Uygulama genelinde Controller veya veritabanı katmanlarında oluşabilecek tüm beklenmeyen
    /// çalışma zamanı hatalarını (Unhandled Exceptions) tek bir noktada yakalar, sistem loglarına kaydeder
    /// ve istemciye (Frontend) yapılandırılmış standart bir JSON hata yanıtı döndürür.
    /// Bu sayede Controller metotları içerisinde tekrarlayan try-catch blokları yazılmasına gerek kalmaz.
    /// </summary>
    public class ExceptionMiddleware
    {
        // HTTP istek hattındaki (Request Pipeline) bir sonraki middleware bileşenini temsil eden temsilci (delegate).
        private readonly RequestDelegate _next;
        // Hata detaylarını konsola veya yapılandırılmış log sağlayıcılarına yazmak için kullanılan logger servisi.
        private readonly ILogger<ExceptionMiddleware> _logger;

        /// <summary>
        /// Bağımlılık Enjeksiyonu (Dependency Injection) ile gerekli pipeline ve loglama bileşenlerini alır.
        /// </summary>
        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Gelen her HTTP isteğini sarmalayarak bir sonraki katmana iletir.
        /// Alt katmanların herhangi birinde istisna fırlatılırsa catch bloğunda yakalayıp standart hata yanıtına dönüştürür.
        /// </summary>
        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                // İsteği pipeline'daki bir sonraki middleware'e (veya Controller'a) aktar.
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                // Yakalanan hatayı sunucu loglarına (Error seviyesinde) kaydet.
                _logger.LogError($"Bir hata oluştu: {ex}");
                // İstemciye anlamlı ve standart formatta JSON yanıtı dön.
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        /// <summary>
        /// Yakalanan istisnayı HTTP 500 (Internal Server Error) durum kodu ve standart JSON şemasıyla
        /// HTTP yanıt akışına (Response Stream) yazar.
        /// </summary>
        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            // İstemcinin kolayca ayrıştırabileceği (parse edebileceği) standart hata modeli.
            var response = new
            {
                statusCode = context.Response.StatusCode,
                message = "Sunucu tarafında beklenmeyen bir hata oluştu.",
                detailed = exception.Message
            };

            var jsonResponse = JsonSerializer.Serialize(response);
            return context.Response.WriteAsync(jsonResponse);
        }
    }
}