# =============================================================================
# ÇOK AŞAMALI (MULTI-STAGE) DOCKERFILE YAPILANDIRMASI
# =============================================================================
# Uygulamanın derlenme (SDK) ve çalışma zamanı (Runtime) aşamalarını birbirinden ayırarak
# nihai konteyner imajının boyutunu küçültür ve saldırı yüzeyini azaltır.

# 1. AŞAMA (BASE): Sadece ASP.NET Core Runtime içeren hafif çalışma zamanı katmanı.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
# Güvenlik için konteyner root yerine yetkisiz "app" kullanıcısı ile çalıştırılır.
USER app
WORKDIR /app
EXPOSE 8080

# 2. AŞAMA (BUILD): Kaynak kodun derlenmesi için tam .NET 8 SDK içeren katman.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Docker önbellekleme (Layer Caching) optimizasyonu: Önce sadece .csproj kopyalanıp NuGet paketleri indirilir.
COPY ["NetworkTopologyTracker.Api/NetworkTopologyTracker.Api.csproj", "NetworkTopologyTracker.Api/"]
RUN dotnet restore "./NetworkTopologyTracker.Api/NetworkTopologyTracker.Api.csproj"
# Ardından tüm kaynak kodlar kopyalanır ve proje derlenir.
COPY . .
WORKDIR "/src/NetworkTopologyTracker.Api"
RUN dotnet build "./NetworkTopologyTracker.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 3. AŞAMA (PUBLISH): Derlenen uygulamanın dağıtıma hazır (Publish) çıktılarının üretilmesi.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./NetworkTopologyTracker.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 4. AŞAMA (FINAL): Publish çıktılarının hafif "base" imajına kopyalanıp uygulamanın başlatılması.
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NetworkTopologyTracker.Api.dll"]