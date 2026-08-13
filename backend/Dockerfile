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
RUN adduser --disabled-password --gecos "" --uid 10001 gameflow \
    && mkdir -p /app/uploads \
    && chown -R gameflow:gameflow /app

COPY --from=build --chown=gameflow:gameflow /app ./

USER gameflow

# Render/Railway PORT ortam değişkeni ile dinlenecek portu bildirir.
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

# Yüklenen dosyalar kalıcı disk olmadan konteyner yeniden başlarken kaybolur.
# Render'da bu yola bir disk bağlanmalıdır (bkz. render.yaml).
VOLUME ["/app/uploads"]

ENTRYPOINT ["dotnet", "GameFlow.Api.dll"]
