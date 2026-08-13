# GameFlow API — çok aşamalı derleme.
# Derleme aşaması SDK'yı kullanır, çalışma aşamasında yalnızca runtime kalır;
# böylece imaj küçük olur ve derleme araçları üretime taşınmaz.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Önce yalnızca proje dosyaları kopyalanır: paket geri yükleme katmanı
# kaynak kod değiştiğinde yeniden çalışmaz (Docker katman önbelleği).
COPY GameFlow.slnx ./
COPY src/GameFlow.Domain/*.csproj src/GameFlow.Domain/
COPY src/GameFlow.Application/*.csproj src/GameFlow.Application/
COPY src/GameFlow.Infrastructure/*.csproj src/GameFlow.Infrastructure/
COPY src/GameFlow.Api/*.csproj src/GameFlow.Api/
RUN dotnet restore

COPY . .
RUN dotnet publish src/GameFlow.Api/GameFlow.Api.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Kök olmayan kullanıcı: konteyner ele geçirilse bile yetki sınırlı kalır.
# .NET runtime imajı hazır bir 'app' kullanıcısıyla (UID 1654) gelir; ayrıca
# kullanıcı oluşturulmaz çünkü bu imajda 'adduser' komutu bulunmaz ve derleme
# o adımda kırılır.
RUN mkdir -p /app/uploads && chown -R app:app /app

COPY --from=build --chown=app:app /app ./

USER app

# Render/Railway PORT ortam değişkeni ile dinlenecek portu bildirir.
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

# Yüklenen dosyalar kalıcı disk olmadan konteyner yeniden başlarken kaybolur.
# Render'da bu yola bir disk bağlanmalıdır (bkz. render.yaml).
VOLUME ["/app/uploads"]

ENTRYPOINT ["dotnet", "GameFlow.Api.dll"]
