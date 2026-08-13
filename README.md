# GameFlow

Oyun geliştirme ekipleri için gerçek zamanlı proje yönetim sistemi. Jira'dan ilham alır,
oyun stüdyolarının iş akışına uyarlanmıştır: görsel/ses varlıkları, seviye tasarımı ve
oynanış testleri birinci sınıf görev türleridir.

**Giriş:** hesaplar yalnızca yönetici tarafından oluşturulur — kayıt ekranı yoktur.

---

## Hızlı başlangıç

```bash
./baslat.sh
```

Veritabanını kontrol eder, backend'i (5080) ve frontend'i (5173) başlatır, Ctrl+C ile
ikisini birlikte kapatır.

| | Adres |
|---|---|
| Arayüz | http://localhost:5173 |
| API dokümanı | http://localhost:5080/docs |
| PostgreSQL | `localhost:5434` · db `gameflow` · kullanıcı `gameflow` |

**Varsayılan yönetici:** `admin@gameflow.dev` / `Admin!2345`
(`backend/src/GameFlow.Api/appsettings.Development.json` → `Seed`)

### Elle başlatma

```bash
cd backend/src/GameFlow.Api && ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://localhost:5080
```

```bash
cd frontend && npm run dev
```

---

## Teknolojiler

**Backend** — ASP.NET Core 10 · Entity Framework Core 10 (Code First) · PostgreSQL 17 ·
Npgsql · JWT + refresh token rotasyonu · SignalR · FluentValidation · Serilog

**Frontend** — React 19 · TypeScript · Vite · Tailwind CSS 4 · Radix UI · TanStack Query ·
Zustand · Axios · dnd-kit · Recharts · date-fns

---

## Mimari

```
backend/src/
├── GameFlow.Domain          Varlıklar, enum'lar, iş istisnaları — hiçbir şeye bağımlı değil
├── GameFlow.Application     İş kuralları, DTO'lar, doğrulayıcılar, servis arayüzleri
├── GameFlow.Infrastructure   EF Core, PostgreSQL, JWT, dosya depolama, şifre özetleme
└── GameFlow.Api             Controller'lar, SignalR hub'ları, middleware, DI birleştirme

frontend/src/
├── components/ui            Tasarım sistemi parçaları (buton, kart, dialog…)
├── components/layout        Uygulama kabuğu (sidebar, topbar)
├── features/<modül>         Her modül kendi API katmanı + ekranlarıyla
├── lib                      API istemcisi, sorgu anahtarları, tarih/format yardımcıları
└── types                    Backend DTO'larıyla birebir eşleşen tipler
```

Bağımlılık yönü tek taraflıdır: `Api → Infrastructure → Application → Domain`.

### Öne çıkan kararlar

**Anahtarlar** — Sıralı GUID v7. Rastgele GUID'in aksine index parçalanmasına yol açmaz.

**Mantıksal silme** — Ana varlıklar (kullanıcı, takım, proje, görev) silinmez, işaretlenir.
Global query filter ile otomatik gizlenir; yorumlar, görev geçmişi ve denetim kayıtları korunur.

**Kanban sıralaması** — `BoardOrder` kesirli tutulur. Sürükle-bırakta iki komşunun ortası
alınır, yani **tek satır** güncellenir. Kayan nokta hassasiyeti tükenirse kolon otomatik
yeniden dengelenir.

**Sürükle-bırak** — Kartın taşınması ile açılması ayrı hedeflere bağlıdır: sol kenardaki
tutamak taşır, gövde tıklanınca görev açılır. Kart sürüklenirken hedef kolonda gerçekten
görünür (kolon yalnızca vurgulanmakla kalmaz); bırakma anındaki yerleşim sunucu yanıtı
beklenmeden önbelleğe yazılır, istek reddedilirse kart eski yerine döner. Kolonlar
daraltılabilir ve pano üstündeki filtreler kartları sunucuya gitmeden süzer.

**Görev anahtarı** (`ODY-42`) — Sayaç `UPDATE ... RETURNING` ile tek atomik ifadede artırılır.
Oku-artır-yaz yaklaşımı eş zamanlı iki oluşturmada aynı numarayı verirdi.

**Takvim** — Görev son tarihleri ve sprint tarihleri ayrı tabloya kopyalanmaz; sorgu anında
kaynaklarından türetilir. Bir görevin tarihi değişince senkronizasyon gerekmez. Takvimde
yalnızca elle eklenen etkinlikler silinebilir; türetilmiş kayıtlar kaynak kayda bağlanır.

**Toplantılar** — Ayrı bir modüldür (`features/meetings`) ve takvimde de görünür. Toplantı
oluşturmak yönetici veya ilgili takım/projenin lideri olmayı gerektirir; katılımcılar
"katılacağım / katılmayacağım" yanıtını kendileri verir.

**Anlık iletim** — Uygulama katmanı SignalR'ı tanımaz; `IRealtimeNotifier` ve `IChatNotifier`
arayüzleri üzerinden konuşur. SignalR devre dışıysa etkisiz uygulamalar kullanılır ve
uygulama REST üzerinden çalışmaya devam eder.

**Zaman** — Tüm `DateTime` alanları UTC'ye normalize edilir (`timestamptz`). Deadline
hesapları sunucuda yapılır; istemci saatine güvenilmez.

---

## Roller

| | Yetkiler |
|---|---|
| **Yönetici** | Kullanıcı/takım/proje oluşturma-silme, lider atama, duyuru yayınlama, toplantı düzenleme, tüm veriye erişim |
| **Takım Lideri** | Görev ve sprint oluşturma, deadline belirleme, görev atama, takımını yönetme, toplantı düzenleme, lider sohbeti |
| **Takım Üyesi** | Kendi görevlerini görme ve durumunu değiştirme, yorum, dosya, takvim etkinliği, toplantı yanıtı, takım sohbeti |

**Hesap açma** — Kayıt ekranı yoktur. Yönetici, **Yönetim paneli → Kullanıcılar → Yeni
kullanıcı** ile hesabı açar: ad, e-posta, geçici şifre, rol, unvan ve doğrudan ekleneceği
takımlar. "İlk girişte şifre değiştirsin" açıkken kullanıcı geçici şifreyle girip kendi
şifresini belirler. Aynı ekrandan şifre sıfırlanır, rol değiştirilir ve hesap kapatılır.

Yetki iki katmanda denetlenir: controller'daki rol politikaları ("bu uç noktayı kim çağırabilir")
ve `IPermissionService` ("tam olarak bu kayda dokunabilir mi").

---

## Test

```bash
cd backend && ./scripts/smoke-test.sh      # 176 REST testi
cd backend && node scripts/signalr-test.mjs # 29 SignalR testi (iki canlı istemci)
```

Duman testi kimlik doğrulama, rol bazlı yetkilendirme, iş kuralları, doğrulama mesajları ve
mantıksal silmeyi uçtan uca doğrular; oluşturduğu kayıtları sonunda temizler.
SignalR testi iki gerçek WebSocket istemcisi bağlar ve mesajlaşma, düzenleme/silme yayını,
"yazıyor" göstergesi, okundu bilgisi, çevrimiçi durum ve anlık bildirimi sınar.

Veritabanını sıfırlamak için (yalnızca yönetici hesabı kalır):

```bash
psql -h localhost -p 5434 -U gameflow -d gameflow -f backend/scripts/clean-test-data.sql
```

---

## Yayına alma

Uygulama iki parçaya ayrılır: **arayüz GitHub Pages'te**, **API ve veritabanı ayrı bir
sunucuda**. Pages yalnızca statik dosya sunar; ASP.NET Core API'yi barındıramaz.

### Arayüz → GitHub Pages

Depo public olmalıdır: Pages, ücretsiz planda private depolarda çalışmaz.

Yayın, derleme çıktısını `gh-pages` dalına gönderen bir scriptle yapılır:

```bash
cd frontend && VITE_API_BASE_URL=https://api-adresiniz ./scripts/deploy-pages.sh
```

Depo ayarlarında **Settings → Pages → Source: Deploy from a branch → `gh-pages` / `/`**
seçili olmalıdır (bir kez).

`VITE_API_BASE_URL` derleme anında pakete gömülür, çalışma anında okunmaz. API adresi
değişirse arayüzün yeniden derlenip yayınlanması gerekir.

Script neden GitHub Actions kullanmıyor: workflow dosyası göndermek, kullanılan kişisel
erişim token'ında `workflow` izni ister. Bu yol yalnızca normal push yetkisiyle çalışır.

### API + veritabanı → Render

`render.yaml`'daki `gameflow-api` ve `gameflow-db` servisleri kullanılır. Render →
**New → Blueprint** → depoyu seçin. `DATABASE_URL` ve `Jwt__Secret` otomatik üretilir;
panelden girilmesi gerekenler:

| Değişken | Değer |
|---|---|
| `Seed__AdminEmail` | İlk yönetici e-postası |
| `Seed__AdminPassword` | İlk yönetici şifresi |
| `Cors__AllowedOrigins__0` | `https://<kullanici>.github.io` |

`Cors__AllowedOrigins__0` yalnızca **kaynak** (origin) olmalıdır: şema dahil, yol ve
sondaki `/` olmadan. `https://kayra-dev.github.io/GameFlow/` yazılırsa tarayıcı bütün
istekleri CORS nedeniyle engeller.

Migration'lar uygulama açılışında otomatik uygulanır. Servis ayağa kalktığında
`/health` adresi `200` dönmelidir.

### Tamamen ücretsiz kurulum

Render'ın ücretsiz PostgreSQL örneği sürelidir ve dolduğunda silinir. Kalıcı ve ücretsiz
bir kurulum için veritabanı [Neon](https://neon.tech)'da tutulur:

1. Neon'da bir proje açın, verdiği `postgres://…` bağlantı dizesini kopyalayın.
2. `render.yaml`'daki `databases:` bloğunu kaldırın (veya blueprint yerine yalnızca web
   servisini oluşturun).
3. Render'da `DATABASE_URL` değerini Neon'un bağlantı dizesiyle **elle** tanımlayın.

`ConnectionStringResolver` `postgres://` biçimini Npgsql formatına çevirir ve SSL'i
zorunlu kılar; Neon SSL istediği için ek ayar gerekmez.

Ücretsiz web servisi 15 dakika hareketsizlikten sonra uyur, sonraki ilk istek 30–60 sn
sürer. `docs/keep-alive.yml` bu gecikmeyi önleyen zamanlanmış bir GitHub Actions işidir;
devreye alma adımları `docs/README.md` içinde.

**Dosya ekleri** — Ücretsiz planda kalıcı disk yoktur; konteynerin dosya sistemi her
yeniden başlatmada sıfırlanır. Bu yüzden üretimde ekler diske değil veritabanına yazılır:

```
FileStorage__Provider=Database
```

Bu ayarla dosyalar `StoredFiles` tablosunda `bytea` olarak durur ve `/api/files/{ad}`
üzerinden sunulur. Uç nokta kimlik doğrulaması istemez — tarayıcı `<img>` isteklerine
`Authorization` başlığı ekleyemez — erişim tahmin edilemez GUID v7 dosya adıyla korunur;
bu, diskten sunulan `/uploads` yolunun güvenlik davranışıyla aynıdır.

Dosya içeriği belleğe alındığından boyut sınırı düşük tutulmalıdır
(`FileStorage__MaxFileSizeBytes`, üretimde 10 MB). Kalıcı disk bağlanabilen bir ortamda
`Provider` ayarını `Local` bırakmak daha verimlidir.

### Giriş

`Seed__AdminPassword` ile açılan hesap sistemdeki **tek** giriş noktasıdır; kayıt ekranı
yoktur. Diğer bütün hesaplar bu hesapla girip **Yönetim paneli → Kullanıcılar → Yeni
kullanıcı** adımından açılır.

## ⚠️ Yayına almadan önce

`appsettings.Development.json` içinde şu ayar **açık**:

```json
"Security": { "StorePasswordsAsPlainText": true }
```

Bu, şifreleri veritabanına düz metin yazar ve **yalnızca geliştirme içindir**. Üretim
ortamında açık bırakılırsa uygulama başlatılmaz (ortam denetimi devrede), ama yayına
almadan önce:

1. Ayarı `false` yapın veya kaldırın.
2. Mevcut şifreleri sıfırlayın — düz metin kayıtlar `plain:` önekiyle durur ve BCrypt'e
   kendiliğinden dönüşmez.
