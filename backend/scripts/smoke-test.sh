#!/usr/bin/env bash
#
# GameFlow API duman testi (smoke test).
#
# API'nin temel akışlarını uçtan uca doğrular: kimlik doğrulama, rol bazlı
# yetkilendirme, doğrulama mesajları, iş kuralları ve mantıksal silme.
# Test sırasında oluşturduğu tüm kayıtları sonunda temizler.
#
# Kullanım:
#   ./scripts/smoke-test.sh
#   API=http://localhost:5080 ADMIN_PASSWORD='...' ./scripts/smoke-test.sh
#
set -uo pipefail

API=${API:-http://localhost:5080}
ADMIN_EMAIL=${ADMIN_EMAIL:-admin@gameflow.dev}
ADMIN_PASSWORD=${ADMIN_PASSWORD:-Admin!2345}

# Testin oluşturduğu kayıtları ayırt etmek için benzersiz ek.
SUFFIX=$$
TEST_EMAIL="duman-test-${SUFFIX}@gameflow.dev"
TEST_USER_PASSWORD='DumanTest1'
TEAM_NAME="Duman Testi ${SUFFIX}"
PROJECT_KEY="DT${SUFFIX: -4}"

PASSED=0
FAILED=0

if [[ -t 1 ]]; then
  GREEN=$'\033[32m'; RED=$'\033[31m'; DIM=$'\033[2m'; BOLD=$'\033[1m'; RESET=$'\033[0m'
else
  GREEN=''; RED=''; DIM=''; BOLD=''; RESET=''
fi

section() { printf '\n%s%s%s\n' "$BOLD" "$1" "$RESET"; }

# assert <açıklama> <beklenen> <gerçek>
assert() {
  if [[ "$2" == "$3" ]]; then
    PASSED=$((PASSED + 1))
    printf '  %s✓%s %s\n' "$GREEN" "$RESET" "$1"
  else
    FAILED=$((FAILED + 1))
    printf '  %s✗%s %s %s(beklenen: %s, gelen: %s)%s\n' \
      "$RED" "$RESET" "$1" "$DIM" "$2" "$3" "$RESET"
  fi
}

# ÖNEMLİ: JSON gövdeleri komut ikamesi içinde satır içi yazılmaz. Bash'in süslü
# parantez genişletmesi ({a,b} -> "a" "b") iç içe tırnaklı bir komut ikamesinde
# JSON'u virgülden bölerek bozar. Bu yüzden her gövde önce bir değişkene atanır.
#
# HTTP durum kodunu döner. Kullanım: status <method> <yol> [token] [gövde]
status() {
  local method=$1 path=$2 token=${3:-} body=${4:-}
  local args=(-s -o /dev/null -w '%{http_code}' -X "$method" "${API}${path}")
  [[ -n $token ]] && args+=(-H "Authorization: Bearer ${token}")
  [[ -n $body ]] && args+=(-H 'Content-Type: application/json' -d "$body")
  curl "${args[@]}"
}

# Yanıt gövdesini döner. Kullanım: request <method> <yol> [token] [gövde]
request() {
  local method=$1 path=$2 token=${3:-} body=${4:-}
  local args=(-s -X "$method" "${API}${path}")
  [[ -n $token ]] && args+=(-H "Authorization: Bearer ${token}")
  [[ -n $body ]] && args+=(-H 'Content-Type: application/json' -d "$body")
  curl "${args[@]}"
}

# JSON gövdesinden alan okur. Kullanım: echo "$json" | field "d['id']"
field() { python3 -c "import sys,json;d=json.load(sys.stdin);print($1)" 2>/dev/null; }

printf '%sGameFlow API duman testi%s  %s%s%s\n' "$BOLD" "$RESET" "$DIM" "$API" "$RESET"

# ---------------------------------------------------------------- Sağlık
section '1. Sağlık kontrolü'

if ! curl -sf --max-time 5 "${API}/health" -o /dev/null; then
  printf '  %s✗%s API ayakta değil. Önce şunu çalıştırın:\n' "$RED" "$RESET"
  printf '      cd src/GameFlow.Api && ASPNETCORE_ENVIRONMENT=Development dotnet run --urls %s\n' "$API"
  exit 1
fi
assert 'API yanıt veriyor' '200' "$(status GET /health)"

# ---------------------------------------------------- Kimlik doğrulama
section '2. Kimlik doğrulama'

BODY_LOGIN="{\"email\":\"${ADMIN_EMAIL}\",\"password\":\"${ADMIN_PASSWORD}\"}"

LOGIN=$(request POST /api/auth/login '' "$BODY_LOGIN")
ADMIN_TOKEN=$(echo "$LOGIN" | field "d['accessToken']")
ADMIN_REFRESH=$(echo "$LOGIN" | field "d['refreshToken']")
ADMIN_ID=$(echo "$LOGIN" | field "d['user']['id']")

if [[ -z $ADMIN_TOKEN ]]; then
  printf '  %s✗%s Yönetici girişi başarısız. ADMIN_PASSWORD doğru mu?\n' "$RED" "$RESET"
  printf '      Yanıt: %s\n' "$LOGIN"
  exit 1
fi

BODY_WRONG_PASSWORD="{\"email\":\"${ADMIN_EMAIL}\",\"password\":\"kesinlikle-yanlis\"}"
BODY_UNKNOWN_USER='{"email":"yok@gameflow.dev","password":"HerhangiBir1"}'

assert 'Yönetici girişi başarılı' '200' "$(status POST /api/auth/login '' "$BODY_LOGIN")"
assert 'Hatalı şifre reddedilir' '401' "$(status POST /api/auth/login '' "$BODY_WRONG_PASSWORD")"
assert 'Olmayan kullanıcı da 401 döner (bilgi sızmaz)' '401' \
  "$(status POST /api/auth/login '' "$BODY_UNKNOWN_USER")"
assert 'Token olmadan /auth/me erişilemez' '401' "$(status GET /api/auth/me)"
assert 'Token ile /auth/me erişilir' '200' "$(status GET /api/auth/me "$ADMIN_TOKEN")"
assert 'Bozuk token reddedilir' '401' "$(status GET /api/auth/me 'gecersiz.token.degeri')"
assert 'Roller listelenir' '200' "$(status GET /api/roles "$ADMIN_TOKEN")"
assert 'Rol sayısı 3' '3' "$(request GET /api/roles "$ADMIN_TOKEN" | field 'len(d)')"

# Refresh token rotasyonu: kullanılan token bir daha çalışmamalı.
BODY_REFRESH="{\"refreshToken\":\"${ADMIN_REFRESH}\"}"

assert 'Refresh token çalışır' '200' "$(status POST /api/auth/refresh '' "$BODY_REFRESH")"
assert 'Kullanılmış refresh token iptal edilir (rotasyon)' '401' \
  "$(status POST /api/auth/refresh '' "$BODY_REFRESH")"

# ------------------------------------------------------------ Doğrulama
section '3. Form doğrulama'

INVALID=$(request POST /api/users "$ADMIN_TOKEN" \
  '{"fullName":"Ab","email":"gecersiz-eposta","password":"123","role":3}')
assert 'Geçersiz veri 400 döner' '400' \
  "$(status POST /api/users "$ADMIN_TOKEN" '{"fullName":"Ab","email":"x","password":"1","role":3}')"
assert 'Hata gövdesi alan bazlı döner' 'True' \
  "$(echo "$INVALID" | field "all(k in d['errors'] for k in ('FullName','Email','Password'))")"
assert 'Hata mesajları Türkçe' 'True' \
  "$(echo "$INVALID" | field "'zorunlu' in str(d['errors']).lower() or 'olmalı' in str(d['errors']).lower()")"

# ------------------------------------------------------------- Kullanıcı
section '4. Kullanıcı yönetimi (yalnızca yönetici)'

NEW_USER=$(request POST /api/users "$ADMIN_TOKEN" "$(cat <<JSON
{"fullName":"Duman Test Kullanıcısı","email":"${TEST_EMAIL}",
 "password":"${TEST_USER_PASSWORD}","role":3,"jobTitle":"Test Mühendisi",
 "mustChangePassword":false}
JSON
)")
TEST_USER_ID=$(echo "$NEW_USER" | field "d['id']")

assert 'Kullanıcı oluşturuldu' 'True' "$([[ -n $TEST_USER_ID ]] && echo True || echo False)"
assert 'Rol Takım Üyesi (3) olarak atandı' '3' "$(echo "$NEW_USER" | field "d['role']")"
BODY_DUPLICATE_USER="{\"fullName\":\"Kopya Kayit\",\"email\":\"${TEST_EMAIL}\",\"password\":\"${TEST_USER_PASSWORD}\",\"role\":3}"

assert 'Aynı e-posta ile tekrar oluşturulamaz' '409' \
  "$(status POST /api/users "$ADMIN_TOKEN" "$BODY_DUPLICATE_USER")"
assert 'Yönetici kendi hesabını silemez' '400' "$(status DELETE "/api/users/${ADMIN_ID}" "$ADMIN_TOKEN")"
assert 'Son yönetici rolü düşürülemez' '400' \
  "$(status PUT "/api/users/${ADMIN_ID}" "$ADMIN_TOKEN" \
     '{"fullName":"Sistem Yöneticisi","role":3,"isActive":true}')"
assert 'Arama çalışıyor' '1' \
  "$(request GET "/api/users?search=duman-test-${SUFFIX}" "$ADMIN_TOKEN" | field "d['totalCount']")"

# --------------------------------------------------------- Yetkilendirme
section '5. Rol bazlı yetkilendirme'

BODY_MEMBER_LOGIN="{\"email\":\"${TEST_EMAIL}\",\"password\":\"${TEST_USER_PASSWORD}\"}"
MEMBER_TOKEN=$(request POST /api/auth/login '' "$BODY_MEMBER_LOGIN" | field "d['accessToken']")

assert 'Takım üyesi kullanıcı listesini okuyabilir' '200' "$(status GET /api/users "$MEMBER_TOKEN")"
assert 'Takım üyesi kullanıcı OLUŞTURAMAZ' '403' \
  "$(status POST /api/users "$MEMBER_TOKEN" \
     '{"fullName":"Izinsiz Kayit","email":"izinsiz@gameflow.dev","password":"Gecerli123","role":3}')"
assert 'Takım üyesi kullanıcı SİLEMEZ' '403' \
  "$(status DELETE "/api/users/${ADMIN_ID}" "$MEMBER_TOKEN")"
assert 'Takım üyesi şifre sıfırlayamaz' '403' \
  "$(status POST "/api/users/${ADMIN_ID}/reset-password" "$MEMBER_TOKEN" '{"newPassword":"Gecerli123"}')"
assert 'Takım üyesi takım oluşturamaz' '403' \
  "$(status POST /api/teams "$MEMBER_TOKEN" '{"name":"Izinsiz Takim","category":1,"colorHex":"#6366F1"}')"
assert 'Takım üyesi proje oluşturamaz' '403' \
  "$(status POST /api/projects "$MEMBER_TOKEN" '{"name":"Izinsiz Proje","key":"IZN","colorHex":"#8B5CF6"}')"

# ----------------------------------------------------------------- Takım
section '6. Takım yönetimi'

BODY_TEAM="{\"name\":\"${TEAM_NAME}\",\"description\":\"Duman testi takımı\",\"category\":1,\"colorHex\":\"#6366F1\",\"iconKey\":\"code\"}"
TEAM=$(request POST /api/teams "$ADMIN_TOKEN" "$BODY_TEAM")
TEAM_ID=$(echo "$TEAM" | field "d['id']")

assert 'Takım oluşturuldu' 'True' "$([[ -n $TEAM_ID ]] && echo True || echo False)"
assert 'Sohbet odası otomatik oluştu' 'True' "$(echo "$TEAM" | field "d['chatRoomId'] is not None")"
BODY_TEAM_DUPLICATE="{\"name\":\"${TEAM_NAME}\",\"category\":1,\"colorHex\":\"#6366F1\"}"

assert 'Aynı adla ikinci takım açılamaz' '409' \
  "$(status POST /api/teams "$ADMIN_TOKEN" "$BODY_TEAM_DUPLICATE")"
assert 'Geçersiz renk reddedilir' '400' \
  "$(status POST /api/teams "$ADMIN_TOKEN" '{"name":"Renk Testi","category":1,"colorHex":"mavi"}')"

# Lider atama, kullanıcının sistem rolünü de yükseltmeli.
BODY_LEADER="{\"userId\":\"${TEST_USER_ID}\"}"
LED=$(request PUT "/api/teams/${TEAM_ID}/leader" "$ADMIN_TOKEN" "$BODY_LEADER")
assert 'Lider atandı' "$TEST_USER_ID" "$(echo "$LED" | field "d['leader']['id']")"
assert 'Lider takıma otomatik üye oldu' '1' "$(echo "$LED" | field "d['memberCount']")"
assert 'Sistem rolü Takım Lideri (2) oldu' '2' \
  "$(request GET "/api/users/${TEST_USER_ID}" "$ADMIN_TOKEN" | field "d['role']")"

# Lider yetkileri yeni token ile tazelenir.
LEADER_TOKEN=$(request POST /api/auth/login '' "$BODY_MEMBER_LOGIN" | field "d['accessToken']")

assert 'ledTeamIds liderliği yansıtıyor' '1' \
  "$(request GET /api/auth/me "$LEADER_TOKEN" | field "len(d['ledTeamIds'])")"
BODY_TEAM_UPDATE="{\"name\":\"${TEAM_NAME}\",\"description\":\"Güncellendi\",\"category\":1,\"colorHex\":\"#6366F1\"}"

assert 'Lider KENDİ takımını güncelleyebilir' '200' \
  "$(status PUT "/api/teams/${TEAM_ID}" "$LEADER_TOKEN" "$BODY_TEAM_UPDATE")"
assert 'Lider takımı SİLEMEZ (yalnızca yönetici)' '403' \
  "$(status DELETE "/api/teams/${TEAM_ID}" "$LEADER_TOKEN")"
BODY_ADD_TEAM_MEMBER="{\"userIds\":[\"${ADMIN_ID}\"]}"

assert 'Lider kendi takımına üye ekleyebilir' '200' \
  "$(status POST "/api/teams/${TEAM_ID}/members" "$LEADER_TOKEN" "$BODY_ADD_TEAM_MEMBER")"
assert 'Aynı üye ikinci kez eklenemez' '400' \
  "$(status POST "/api/teams/${TEAM_ID}/members" "$LEADER_TOKEN" "$BODY_ADD_TEAM_MEMBER")"
assert 'Takım detayında 2 üye var' '2' \
  "$(request GET "/api/teams/${TEAM_ID}" "$ADMIN_TOKEN" | field "d['memberCount']")"

# ---------------------------------------------------------------- Proje
section '7. Proje yönetimi'

PROJECT=$(request POST /api/projects "$ADMIN_TOKEN" "$(cat <<JSON
{"name":"Duman Testi Projesi ${SUFFIX}","key":"${PROJECT_KEY}",
 "description":"Duman testi","status":2,"colorHex":"#8B5CF6",
 "genre":"Roguelike","platforms":"PC","startDate":"2026-01-15T00:00:00Z",
 "targetReleaseDate":"2027-06-01T00:00:00Z"}
JSON
)")
PROJECT_ID=$(echo "$PROJECT" | field "d['id']")

assert 'Proje oluşturuldu' 'True' "$([[ -n $PROJECT_ID ]] && echo True || echo False)"
assert 'Proje anahtarı büyük harfe çevrildi' "$(echo "$PROJECT_KEY" | tr '[:lower:]' '[:upper:]')" \
  "$(echo "$PROJECT" | field "d['key']")"
assert 'Oluşturan otomatik üye oldu' '1' "$(echo "$PROJECT" | field "d['memberCount']")"
PROJECT_KEY_LOWER=$(echo "$PROJECT_KEY" | tr '[:upper:]' '[:lower:]')
BODY_PROJECT_DUPLICATE="{\"name\":\"Kopya Proje\",\"key\":\"${PROJECT_KEY_LOWER}\",\"colorHex\":\"#8B5CF6\"}"

assert 'Aynı anahtar (küçük harfle) reddedilir' '409' \
  "$(status POST /api/projects "$ADMIN_TOKEN" "$BODY_PROJECT_DUPLICATE")"
assert 'Tek karakterlik anahtar reddedilir' '400' \
  "$(status POST /api/projects "$ADMIN_TOKEN" '{"name":"Kisa Anahtar","key":"X","colorHex":"#8B5CF6"}')"
assert 'Bitiş tarihi başlangıçtan önce olamaz' '400' \
  "$(status POST /api/projects "$ADMIN_TOKEN" \
     '{"name":"Tarih Testi","key":"TRH","colorHex":"#8B5CF6","startDate":"2027-01-01T00:00:00Z","targetReleaseDate":"2026-01-01T00:00:00Z"}')"

assert 'Üye olmayan proje detayını göremez' '403' \
  "$(status GET "/api/projects/${PROJECT_ID}" "$LEADER_TOKEN")"
assert 'Üye olmayanın proje listesi boş' '0' \
  "$(request GET /api/projects "$LEADER_TOKEN" | field 'len(d)')"
BODY_ADD_PROJECT_MEMBER="{\"userIds\":[\"${TEST_USER_ID}\"],\"isManager\":false}"

assert 'Projeye üye eklenir' '2' \
  "$(request POST "/api/projects/${PROJECT_ID}/members" "$ADMIN_TOKEN" "$BODY_ADD_PROJECT_MEMBER" \
     | field "d['memberCount']")"
assert 'Üye artık detayı görebilir' '200' \
  "$(status GET "/api/projects/${PROJECT_ID}" "$LEADER_TOKEN")"
BODY_PROJECT_UNAUTHORIZED='{"name":"Yetkisiz Guncelleme","status":2,"colorHex":"#8B5CF6"}'

assert 'Sıradan üye proje ayarını değiştiremez' '403' \
  "$(status PUT "/api/projects/${PROJECT_ID}" "$LEADER_TOKEN" "$BODY_PROJECT_UNAUTHORIZED")"
assert 'Proje yöneticiliği verilir' '204' \
  "$(status PUT "/api/projects/${PROJECT_ID}/members/${TEST_USER_ID}/manager?isManager=true" "$ADMIN_TOKEN")"
BODY_PROJECT_UPDATE="{\"name\":\"Duman Testi Projesi ${SUFFIX}\",\"status\":3,\"colorHex\":\"#8B5CF6\"}"

assert 'Proje yöneticisi ayarı değiştirebilir' '200' \
  "$(status PUT "/api/projects/${PROJECT_ID}" "$LEADER_TOKEN" "$BODY_PROJECT_UPDATE")"
# Bu noktada projede iki yönetici var (oluşturan + terfi ettirilen üye).
# Biri kaldırılabilir, ikincisi kaldırılamaz.
assert 'İki yöneticiden biri kaldırılabilir' '204' \
  "$(status PUT "/api/projects/${PROJECT_ID}/members/${ADMIN_ID}/manager?isManager=false" "$ADMIN_TOKEN")"
assert 'Son proje yöneticisi kaldırılamaz' '400' \
  "$(status PUT "/api/projects/${PROJECT_ID}/members/${TEST_USER_ID}/manager?isManager=false" "$ADMIN_TOKEN")"


# ---------------------------------------------------------------- Görevler
section '8. Görev yönetimi ve Kanban'

# Yetki sınırlarını sınamak için hiçbir projeye üye olmayan bir hesap açılır.
OUTSIDER_EMAIL="duman-dis-${SUFFIX}@gameflow.dev"
BODY_OUTSIDER="{\"fullName\":\"Proje Disi Kullanici\",\"email\":\"${OUTSIDER_EMAIL}\",\"password\":\"${TEST_USER_PASSWORD}\",\"role\":3,\"mustChangePassword\":false}"
OUTSIDER_ID=$(request POST /api/users "$ADMIN_TOKEN" "$BODY_OUTSIDER" | field "d['id']")
BODY_OUTSIDER_LOGIN="{\"email\":\"${OUTSIDER_EMAIL}\",\"password\":\"${TEST_USER_PASSWORD}\"}"
OUTSIDER_TOKEN=$(request POST /api/auth/login '' "$BODY_OUTSIDER_LOGIN" | field "d['accessToken']")

# Görev oluşturmak için proje üyeliği + lider/yönetici yetkisi gerekir.
BODY_LABEL='{"name":"duman-etiket","colorHex":"#F97316"}'
LABEL_ID=$(request POST "/api/projects/${PROJECT_ID}/labels" "$ADMIN_TOKEN" "$BODY_LABEL" | field "d['id']")
assert 'Etiket oluşturuldu' 'True' "$([[ -n $LABEL_ID ]] && echo True || echo False)"

BODY_TASK="{\"projectId\":\"${PROJECT_ID}\",\"title\":\"Zıplama mekaniği düzeltmesi\",\"description\":\"Çift zıplama hatası\",\"status\":1,\"priority\":4,\"type\":2,\"assigneeId\":\"${TEST_USER_ID}\",\"labelIds\":[\"${LABEL_ID}\"],\"checklistItems\":[\"Hatayı yeniden üret\",\"Düzeltmeyi yaz\",\"Testi ekle\"],\"estimatedHours\":6.5,\"storyPoints\":5}"
TASK=$(request POST /api/work-items "$ADMIN_TOKEN" "$BODY_TASK")
TASK_ID=$(echo "$TASK" | field "d['id']")
TASK_KEY=$(echo "$TASK" | field "d['key']")

assert 'Görev oluşturuldu' 'True' "$([[ -n $TASK_ID ]] && echo True || echo False)"
assert 'Görev anahtarı proje anahtarıyla üretildi' 'True' \
  "$(echo "$TASK" | field "d['key'].startswith('${PROJECT_KEY}-')")"
assert 'Kontrol listesi 3 madde' '3' "$(echo "$TASK" | field "len(d['checklistItems'])")"
assert 'Etiket bağlandı' '1' "$(echo "$TASK" | field "len(d['labels'])")"
assert 'Atanan kişi kaydedildi' "$TEST_USER_ID" "$(echo "$TASK" | field "d['assignee']['id']")"

# İkinci görev: anahtar numarası artmalı.
BODY_TASK2="{\"projectId\":\"${PROJECT_ID}\",\"title\":\"Ana menü müziği\",\"status\":2,\"priority\":3,\"type\":7}"
TASK2=$(request POST /api/work-items "$ADMIN_TOKEN" "$BODY_TASK2")
TASK2_ID=$(echo "$TASK2" | field "d['id']")
assert 'İkinci görevin numarası arttı' 'True' \
  "$(python3 -c "
k1='$TASK_KEY'.rsplit('-',1)[1]
k2='$(echo "$TASK2" | field "d['key']")'.rsplit('-',1)[1]
print(int(k2) == int(k1) + 1)
")"

assert 'Anahtarla erişim çalışıyor' "$TASK_ID" \
  "$(request GET "/api/work-items/by-key/${TASK_KEY}" "$ADMIN_TOKEN" | field "d['id']")"

# Proje üyesi olmayan kullanıcıya görev atanamaz.
BODY_INVALID_ASSIGN="{\"projectId\":\"${PROJECT_ID}\",\"title\":\"Yetkisiz atama denemesi\",\"assigneeId\":\"${OUTSIDER_ID}\"}"
assert 'Proje üyesi olmayana görev atanamaz' '400' \
  "$(status POST /api/work-items "$ADMIN_TOKEN" "$BODY_INVALID_ASSIGN")"

BODY_BAD_DATES="{\"projectId\":\"${PROJECT_ID}\",\"title\":\"Tarih testi görevi\",\"startDate\":\"2027-01-01T00:00:00Z\",\"dueDate\":\"2026-01-01T00:00:00Z\"}"
assert 'Bitiş tarihi başlangıçtan önce olamaz' '400' \
  "$(status POST /api/work-items "$ADMIN_TOKEN" "$BODY_BAD_DATES")"

# Kanban panosu: 7 kolon, kartlar doğru kolonlarda.
BOARD=$(request GET "/api/work-items/board?projectId=${PROJECT_ID}" "$ADMIN_TOKEN")
assert 'Pano 7 kolon döner' '7' "$(echo "$BOARD" | field "len(d['columns'])")"
assert 'Kolon başlıkları Türkçe' 'Bekliyor' "$(echo "$BOARD" | field "d['columns'][0]['title']")"
assert 'İlk kolonda 1 kart' '1' "$(echo "$BOARD" | field "d['columns'][0]['totalCount']")"
assert 'İkinci kolonda 1 kart' '1' "$(echo "$BOARD" | field "d['columns'][1]['totalCount']")"

# Sürükle-bırak: görevi "Devam Ediyor" kolonuna taşı.
BODY_MOVE='{"targetStatus":3}'
MOVED=$(request PUT "/api/work-items/${TASK_ID}/move" "$ADMIN_TOKEN" "$BODY_MOVE")
assert 'Sürükle-bırak durumu güncelledi' '3' "$(echo "$MOVED" | field "d['status']")"

# Tamamlandı -> CompletedAt dolar, geri alınınca temizlenir.
BODY_DONE='{"status":6}'
assert 'Tamamlandı durumunda completedAt dolar' 'True' \
  "$(request PUT "/api/work-items/${TASK_ID}/status" "$ADMIN_TOKEN" "$BODY_DONE" > /dev/null; \
     request GET "/api/work-items/${TASK_ID}" "$ADMIN_TOKEN" | field "d['completedAt'] is not None")"
BODY_REOPEN='{"status":3}'
assert 'Geri alındığında completedAt temizlenir' 'True' \
  "$(request PUT "/api/work-items/${TASK_ID}/status" "$ADMIN_TOKEN" "$BODY_REOPEN" > /dev/null; \
     request GET "/api/work-items/${TASK_ID}" "$ADMIN_TOKEN" | field "d['completedAt'] is None")"

# Atanan kişi kendi görevinin durumunu değiştirebilir ama silemez.
assert 'Atanan kişi durum değiştirebilir' '200' \
  "$(status PUT "/api/work-items/${TASK_ID}/status" "$LEADER_TOKEN" '{"status":5}')"
assert 'Atanmayan sıradan üye durum değiştiremez' '403' \
  "$(status PUT "/api/work-items/${TASK2_ID}/status" "$OUTSIDER_TOKEN" '{"status":3}')"

# Kontrol listesi ve yorumlar.
BODY_CHECK='{"text":"Regresyon testi çalıştır"}'
assert 'Kontrol listesine madde eklenir' '4' \
  "$(request POST "/api/work-items/${TASK_ID}/checklist" "$ADMIN_TOKEN" "$BODY_CHECK" | field 'len(d)')"

BODY_COMMENT='{"content":"Çift zıplama hatası ivme sıfırlanmadığı için oluşuyor."}'
COMMENT=$(request POST "/api/work-items/${TASK_ID}/comments" "$LEADER_TOKEN" "$BODY_COMMENT")
COMMENT_ID=$(echo "$COMMENT" | field "d['id']")
assert 'Yorum eklenir' 'True' "$([[ -n $COMMENT_ID ]] && echo True || echo False)"

BODY_COMMENT_EDIT='{"content":"Düzeltildi: ivme sıfırlaması eklendi."}'
assert 'Yorum sahibi düzenleyebilir' 'True' \
  "$(request PUT "/api/work-items/${TASK_ID}/comments/${COMMENT_ID}" "$LEADER_TOKEN" "$BODY_COMMENT_EDIT" \
     | field "d['isEdited']")"
assert 'Başkasının yorumu düzenlenemez' '403' \
  "$(status PUT "/api/work-items/${TASK_ID}/comments/${COMMENT_ID}" "$OUTSIDER_TOKEN" "$BODY_COMMENT_EDIT")"

# Dosya yükleme.
UPLOAD_FILE=$(mktemp -t gameflow-test).png
printf '\x89PNG\r\n\x1a\n test' > "$UPLOAD_FILE"
UPLOADED=$(curl -s -X POST "${API}/api/work-items/${TASK_ID}/attachments" \
  -H "Authorization: Bearer ${ADMIN_TOKEN}" -F "file=@${UPLOAD_FILE}")
ATTACHMENT_URL=$(echo "$UPLOADED" | field "d['url']")
assert 'Dosya yüklenir ve kategorisi resim (1)' '1' "$(echo "$UPLOADED" | field "d['category']")"
assert 'Yüklenen dosya sunucudan indirilebilir' '200' "$(status GET "$ATTACHMENT_URL" "$ADMIN_TOKEN")"
rm -f "$UPLOAD_FILE"

# İzin verilmeyen uzantı reddedilir.
BAD_FILE=$(mktemp -t gameflow-test).exe
printf 'MZ' > "$BAD_FILE"
assert 'İzin verilmeyen uzantı reddedilir' '400' \
  "$(curl -s -o /dev/null -w '%{http_code}' -X POST "${API}/api/work-items/${TASK_ID}/attachments" \
     -H "Authorization: Bearer ${ADMIN_TOKEN}" -F "file=@${BAD_FILE}")"
rm -f "$BAD_FILE"

# Aktivite geçmişi ve bildirimler.
assert 'Aktivite geçmişi kaydedildi' 'True' \
  "$(request GET "/api/work-items/${TASK_ID}" "$ADMIN_TOKEN" | field "len(d['activities']) >= 3")"

# ------------------------------------------------------------- Deadline
section '9. Deadline sistemi'

# Günün SONUNA ayarlanır: gün ortasına ayarlansaydı test o saatten sonra
# çalıştırıldığında görev haklı olarak "gecikmiş" sayılır ve sayım kayardı.
TODAY=$(python3 -c "import datetime;print((datetime.datetime.now(datetime.UTC).replace(hour=23,minute=59,second=0,microsecond=0)).strftime('%Y-%m-%dT%H:%M:%SZ'))")
TOMORROW=$(python3 -c "import datetime;print((datetime.datetime.now(datetime.UTC)+datetime.timedelta(days=3)).strftime('%Y-%m-%dT%H:%M:%SZ'))")
PAST=$(python3 -c "import datetime;print((datetime.datetime.now(datetime.UTC)-datetime.timedelta(days=5)).strftime('%Y-%m-%dT%H:%M:%SZ'))")

BODY_DUE_TODAY="{\"projectId\":\"${PROJECT_ID}\",\"title\":\"Bugün bitecek görev\",\"dueDate\":\"${TODAY}\"}"
BODY_DUE_SOON="{\"projectId\":\"${PROJECT_ID}\",\"title\":\"Yaklaşan görev\",\"dueDate\":\"${TOMORROW}\"}"
BODY_OVERDUE="{\"projectId\":\"${PROJECT_ID}\",\"title\":\"Gecikmiş görev\",\"dueDate\":\"${PAST}\"}"

request POST /api/work-items "$ADMIN_TOKEN" "$BODY_DUE_TODAY" > /dev/null
request POST /api/work-items "$ADMIN_TOKEN" "$BODY_DUE_SOON" > /dev/null
OVERDUE_TASK=$(request POST /api/work-items "$ADMIN_TOKEN" "$BODY_OVERDUE")

assert 'Gecikmiş görev isOverdue=true' 'True' "$(echo "$OVERDUE_TASK" | field "d['isOverdue']")"
assert 'Gecikmiş görevde daysUntilDue negatif' 'True' \
  "$(echo "$OVERDUE_TASK" | field "d['daysUntilDue'] < 0")"

DEADLINES=$(request GET "/api/work-items/deadlines?projectId=${PROJECT_ID}&upcomingDays=7" "$ADMIN_TOKEN")
assert 'Bugün bitecek görev listelendi' '1' "$(echo "$DEADLINES" | field "len(d['dueToday'])")"
assert 'Yaklaşan görev listelendi' '1' "$(echo "$DEADLINES" | field "len(d['upcoming'])")"
assert 'Gecikmiş görev listelendi' '1' "$(echo "$DEADLINES" | field "len(d['overdue'])")"
OVERDUE_LIST=$(request GET "/api/work-items?projectId=${PROJECT_ID}&onlyOverdue=true" "$ADMIN_TOKEN")
assert 'Sadece gecikmiş filtresi çalışıyor' '1' "$(echo "$OVERDUE_LIST" | field "d['totalCount']")"
assert 'Filtre doğru görevi döndürdü' 'Gecikmiş görev' \
  "$(echo "$OVERDUE_LIST" | field "d['items'][0]['title']")"

# ---------------------------------------------------------------- Sprint
section '10. Sprint sistemi'

SPRINT_START=$(python3 -c "import datetime;print(datetime.datetime.now(datetime.UTC).strftime('%Y-%m-%dT00:00:00Z'))")
SPRINT_END=$(python3 -c "import datetime;print((datetime.datetime.now(datetime.UTC)+datetime.timedelta(days=14)).strftime('%Y-%m-%dT00:00:00Z'))")

BODY_SPRINT="{\"projectId\":\"${PROJECT_ID}\",\"name\":\"Duman Sprinti 1\",\"goal\":\"Zıplama ve ses işleri\",\"startDate\":\"${SPRINT_START}\",\"endDate\":\"${SPRINT_END}\"}"
SPRINT=$(request POST /api/sprints "$ADMIN_TOKEN" "$BODY_SPRINT")
SPRINT_ID=$(echo "$SPRINT" | field "d['id']")

assert 'Sprint oluşturuldu (durum Planlandı=1)' '1' "$(echo "$SPRINT" | field "d['status']")"

BODY_SPRINT_BAD="{\"projectId\":\"${PROJECT_ID}\",\"name\":\"Çok Uzun Sprint\",\"startDate\":\"${SPRINT_START}\",\"endDate\":\"2028-01-01T00:00:00Z\"}"
assert '60 günden uzun sprint reddedilir' '400' \
  "$(status POST /api/sprints "$ADMIN_TOKEN" "$BODY_SPRINT_BAD")"

assert 'Sprint başlatılır (durum Aktif=2)' '2' \
  "$(request POST "/api/sprints/${SPRINT_ID}/start" "$ADMIN_TOKEN" '' | field "d['status']")"
assert 'Zaten başlamış sprint tekrar başlatılamaz' '400' \
  "$(status POST "/api/sprints/${SPRINT_ID}/start" "$ADMIN_TOKEN")"

# İkinci sprint aynı projede aktif olamaz.
BODY_SPRINT2="{\"projectId\":\"${PROJECT_ID}\",\"name\":\"Duman Sprinti 2\",\"startDate\":\"${SPRINT_START}\",\"endDate\":\"${SPRINT_END}\"}"
SPRINT2_ID=$(request POST /api/sprints "$ADMIN_TOKEN" "$BODY_SPRINT2" | field "d['id']")
assert 'Aynı projede ikinci aktif sprint engellenir' '400' \
  "$(status POST "/api/sprints/${SPRINT2_ID}/start" "$ADMIN_TOKEN")"
assert 'Aktif sprint silinemez' '400' "$(status DELETE "/api/sprints/${SPRINT_ID}" "$ADMIN_TOKEN")"

# Görevleri sprinte al: biri tamamlanmış, biri devam ediyor.
BODY_TO_SPRINT="{\"title\":\"Zıplama mekaniği düzeltmesi\",\"priority\":4,\"type\":2,\"assigneeId\":\"${TEST_USER_ID}\",\"sprintId\":\"${SPRINT_ID}\",\"storyPoints\":5,\"labelIds\":[]}"
request PUT "/api/work-items/${TASK_ID}" "$ADMIN_TOKEN" "$BODY_TO_SPRINT" > /dev/null
BODY_TO_SPRINT2="{\"title\":\"Ana menü müziği\",\"priority\":3,\"type\":7,\"sprintId\":\"${SPRINT_ID}\",\"storyPoints\":3,\"labelIds\":[]}"
request PUT "/api/work-items/${TASK2_ID}" "$ADMIN_TOKEN" "$BODY_TO_SPRINT2" > /dev/null
request PUT "/api/work-items/${TASK2_ID}/status" "$ADMIN_TOKEN" '{"status":6}' > /dev/null

REPORT=$(request GET "/api/sprints/${SPRINT_ID}/report" "$ADMIN_TOKEN")
assert 'Sprint raporu 2 görev sayıyor' '2' "$(echo "$REPORT" | field "d['totalTaskCount']")"
assert 'Rapor 1 tamamlanmış görev sayıyor' '1' "$(echo "$REPORT" | field "d['completedTaskCount']")"
assert 'Rapor ilerlemesi %50' '50' "$(echo "$REPORT" | field "d['progressPercent']")"
assert 'Tamamlanan puan 3' '3' "$(echo "$REPORT" | field "d['completedStoryPoints']")"
assert 'Toplam puan 8' '8' "$(echo "$REPORT" | field "d['totalStoryPoints']")"
assert 'Durum dağılımı döner' 'True' "$(echo "$REPORT" | field "len(d['statusBreakdown']) >= 2")"
assert 'Üye katkıları döner' 'True' "$(echo "$REPORT" | field "len(d['memberContributions']) >= 1")"

# Sprinti tamamla: bitmemiş görev backlog'a dönmeli.
BODY_COMPLETE='{"retrospectiveNotes":"Zıplama işi sarktı, ses işi bitti."}'
COMPLETED=$(request POST "/api/sprints/${SPRINT_ID}/complete" "$ADMIN_TOKEN" "$BODY_COMPLETE")
assert 'Sprint tamamlandı (durum=3)' '3' "$(echo "$COMPLETED" | field "d['status']")"
assert 'Bitmemiş görev backloga döndü' 'True' \
  "$(request GET "/api/work-items/${TASK_ID}" "$ADMIN_TOKEN" | field "d['sprintId'] is None")"
assert 'Tamamlanmış sprint düzenlenemez' '400' \
  "$(status PUT "/api/sprints/${SPRINT_ID}" "$ADMIN_TOKEN" "$BODY_SPRINT")"

# ------------------------------------------------------------ Bildirimler
section '11. Bildirim kayıtları'

assert 'Görev atamasında bildirim üretildi' 'True' \
  "$(request GET /api/notifications "$LEADER_TOKEN" | field "d['totalCount'] > 0")"


# ------------------------------------------------------------------ Sohbet
section '12. Sohbet sistemi'

ROOMS=$(request GET /api/chat/rooms "$ADMIN_TOKEN")
assert 'Takım ve proje odaları listelenir' 'True' "$(echo "$ROOMS" | field "len(d) >= 2")"

# DİKKAT: "ilk takım odası" seçilmez. Yönetici tüm odaları gördüğü için önceki
# koşulardan kalan odalar listede olabilir; o zaman lider kullanıcı üyesi olmadığı
# bir odaya mesaj atmaya çalışır ve test yanlışlıkla kırılır. Bu koşuda oluşturulan
# takımın odası kimliğe göre seçilir.
TEAM_ROOM_ID=$(echo "$ROOMS" | field "[r['id'] for r in d if r['teamId']=='${TEAM_ID}'][0]")
assert 'Takım odası bulundu' 'True' "$([[ -n $TEAM_ROOM_ID ]] && echo True || echo False)"

BODY_MSG='{"content":"Zıplama düzeltmesi bu sprintte bitmeli."}'
MSG=$(request POST "/api/chat/rooms/${TEAM_ROOM_ID}/messages" "$ADMIN_TOKEN" "$BODY_MSG")
MSG_ID=$(echo "$MSG" | field "d['id']")
assert 'Mesaj gönderilir' 'True' "$([[ -n $MSG_ID ]] && echo True || echo False)"
assert 'Gönderen kendi mesajını okumuş sayılır' 'True' "$(echo "$MSG" | field "d['isReadByMe']")"

BODY_MSG_EDIT='{"content":"Zıplama düzeltmesi bu sprintte bitmeli (güncellendi)."}'
assert 'Mesaj düzenlenir' 'True' \
  "$(request PUT "/api/chat/rooms/${TEAM_ROOM_ID}/messages/${MSG_ID}" "$ADMIN_TOKEN" "$BODY_MSG_EDIT" \
     | field "d['isEdited']")"
assert 'Başkasının mesajı düzenlenemez' '403' \
  "$(status PUT "/api/chat/rooms/${TEAM_ROOM_ID}/messages/${MSG_ID}" "$LEADER_TOKEN" "$BODY_MSG_EDIT")"

BODY_REPLY="{\"content\":\"Ben de öyle düşünüyorum.\",\"replyToMessageId\":\"${MSG_ID}\"}"
REPLY=$(request POST "/api/chat/rooms/${TEAM_ROOM_ID}/messages" "$LEADER_TOKEN" "$BODY_REPLY")
assert 'Yanıt mesajı önizleme taşır' 'True' "$(echo "$REPLY" | field "d['replyToPreview'] is not None")"

HISTORY=$(request GET "/api/chat/rooms/${TEAM_ROOM_ID}/messages?pageSize=10" "$ADMIN_TOKEN")
assert 'Geçmiş 2 mesaj döner' '2' "$(echo "$HISTORY" | field "len(d['items'])")"
assert 'Geçmiş eskiden yeniye sıralı' 'True' \
  "$(echo "$HISTORY" | field "d['items'][0]['createdAt'] <= d['items'][1]['createdAt']")"

assert 'Okunmamış mesaj sayılır' 'True' \
  "$(request GET /api/chat/rooms "$ADMIN_TOKEN" \
     | field "[r['unreadCount'] for r in d if r['id']=='${TEAM_ROOM_ID}'][0] >= 1")"
assert 'Okundu işaretlenince sıfırlanır' '0' \
  "$(request PUT "/api/chat/rooms/${TEAM_ROOM_ID}/read" "$ADMIN_TOKEN" '{"messageIds":[]}')"
assert 'Okundu bilgisi listelenir' 'True' \
  "$(request GET "/api/chat/rooms/${TEAM_ROOM_ID}/messages/${MSG_ID}/reads" "$ADMIN_TOKEN" \
     | field "len(d) >= 1")"

assert 'Proje dışı kullanıcı takım odasına erişemez' '403' \
  "$(status GET "/api/chat/rooms/${TEAM_ROOM_ID}/messages" "$OUTSIDER_TOKEN")"
assert 'Lider sohbetine lider erişebilir' '200' "$(status GET /api/chat/rooms/leaders "$LEADER_TOKEN")"
assert 'Lider sohbetine sıradan üye erişemez' '403' \
  "$(status GET /api/chat/rooms/leaders "$OUTSIDER_TOKEN")"

# Sohbette dosya paylaşımı.
CHAT_FILE=$(mktemp -t gameflow-chat).png
printf '\x89PNG\r\n\x1a\n sohbet' > "$CHAT_FILE"
SHARED=$(curl -s -X POST "${API}/api/chat/rooms/${TEAM_ROOM_ID}/attachments" \
  -H "Authorization: Bearer ${ADMIN_TOKEN}" -F "file=@${CHAT_FILE}" -F "caption=Konsept görsel")
assert 'Sohbette dosya paylaşılır' '1' "$(echo "$SHARED" | field "len(d['attachments'])")"
assert 'Dosya mesajı başlık taşır' 'Konsept görsel' "$(echo "$SHARED" | field "d['content']")"
rm -f "$CHAT_FILE"

assert 'Mesaj silinir (mantıksal)' '204' \
  "$(status DELETE "/api/chat/rooms/${TEAM_ROOM_ID}/messages/${MSG_ID}" "$ADMIN_TOKEN")"
assert 'Silinen mesaj geçmişte görünmez' 'True' \
  "$(request GET "/api/chat/rooms/${TEAM_ROOM_ID}/messages" "$ADMIN_TOKEN" \
     | field "all(m['id'] != '${MSG_ID}' for m in d['items'])")"

# ----------------------------------------------------------------- Takvim
section '13. Takvim ve toplantılar'

CAL_FROM=$(python3 -c "import datetime;print((datetime.datetime.now(datetime.UTC)-datetime.timedelta(days=30)).strftime('%Y-%m-%dT00:00:00Z'))")
CAL_TO=$(python3 -c "import datetime;print((datetime.datetime.now(datetime.UTC)+datetime.timedelta(days=30)).strftime('%Y-%m-%dT00:00:00Z'))")

CAL=$(request GET "/api/calendar?from=${CAL_FROM}&to=${CAL_TO}&projectId=${PROJECT_ID}" "$ADMIN_TOKEN")
assert 'Takvim görev son tarihlerini içerir' 'True' \
  "$(echo "$CAL" | field "any(i['type'] == 3 for i in d)")"
assert 'Takvim sprint tarihlerini içerir' 'True' \
  "$(echo "$CAL" | field "any(i['type'] in (4,5) for i in d)")"
assert 'Gecikmiş görev kırmızı renkte' 'True' \
  "$(echo "$CAL" | field "any(i['type']==3 and i['colorHex']=='#EF4444' for i in d)")"
assert 'Takvim tarihe göre sıralı' 'True' \
  "$(echo "$CAL" | field "all(d[i]['startsAt'] <= d[i+1]['startsAt'] for i in range(len(d)-1))")"

BODY_BAD_RANGE_FROM='2026-01-01T00:00:00Z'
assert 'Çok geniş takvim aralığı reddedilir' '400' \
  "$(status GET "/api/calendar?from=${BODY_BAD_RANGE_FROM}&to=2030-01-01T00:00:00Z" "$ADMIN_TOKEN")"

MEET_START=$(python3 -c "import datetime;print((datetime.datetime.now(datetime.UTC)+datetime.timedelta(days=2)).strftime('%Y-%m-%dT10:00:00Z'))")
MEET_END=$(python3 -c "import datetime;print((datetime.datetime.now(datetime.UTC)+datetime.timedelta(days=2)).strftime('%Y-%m-%dT11:00:00Z'))")
BODY_MEETING="{\"title\":\"Sprint planlama\",\"description\":\"Sonraki sprint kapsamı\",\"startsAt\":\"${MEET_START}\",\"endsAt\":\"${MEET_END}\",\"meetingUrl\":\"https://meet.example.com/abc\",\"projectId\":\"${PROJECT_ID}\"}"
MEETING=$(request POST /api/meetings "$ADMIN_TOKEN" "$BODY_MEETING")
MEETING_ID=$(echo "$MEETING" | field "d['id']")

assert 'Toplantı oluşturuldu' 'True' "$([[ -n $MEETING_ID ]] && echo True || echo False)"
assert 'Proje üyeleri katılımcı yapıldı' 'True' "$(echo "$MEETING" | field "len(d['attendees']) >= 2")"
assert 'Düzenleyenin katılımı otomatik onaylı' 'True' "$(echo "$MEETING" | field "d['myResponse'] == True")"

BODY_BAD_URL="{\"title\":\"Bozuk bağlantı\",\"startsAt\":\"${MEET_START}\",\"endsAt\":\"${MEET_END}\",\"meetingUrl\":\"javascript:alert(1)\"}"
assert 'Geçersiz toplantı bağlantısı reddedilir' '400' \
  "$(status POST /api/meetings "$ADMIN_TOKEN" "$BODY_BAD_URL")"

BODY_LONG="{\"title\":\"Çok uzun toplantı\",\"startsAt\":\"${MEET_START}\",\"endsAt\":\"2030-01-01T00:00:00Z\"}"
assert '24 saatten uzun toplantı reddedilir' '400' \
  "$(status POST /api/meetings "$ADMIN_TOKEN" "$BODY_LONG")"

assert 'Katılımcı yanıt verebilir' 'False' \
  "$(request POST "/api/meetings/${MEETING_ID}/respond" "$LEADER_TOKEN" '{"isAccepted":false}' \
     | field "str(d['myResponse'])")"
assert 'Katılımcı olmayan yanıt veremez' '403' \
  "$(status POST "/api/meetings/${MEETING_ID}/respond" "$OUTSIDER_TOKEN" '{"isAccepted":true}')"
assert 'Toplantı takvimde görünür' 'True' \
  "$(request GET "/api/calendar?from=${CAL_FROM}&to=${CAL_TO}&projectId=${PROJECT_ID}" "$ADMIN_TOKEN" \
     | field "any(i['type'] == 2 for i in d)")"

BODY_EVENT="{\"title\":\"Demo sunumu\",\"type\":8,\"startsAt\":\"${MEET_START}\",\"colorHex\":\"#EC4899\",\"isAllDay\":true,\"projectId\":\"${PROJECT_ID}\"}"
EVENT_ID=$(request POST /api/calendar/events "$ADMIN_TOKEN" "$BODY_EVENT" | field "d['id']")
assert 'Elle etkinlik eklenir' 'True' "$([[ -n $EVENT_ID ]] && echo True || echo False)"
assert 'Etkinlik takvimde görünür' 'True' \
  "$(request GET "/api/calendar?from=${CAL_FROM}&to=${CAL_TO}&projectId=${PROJECT_ID}" "$ADMIN_TOKEN" \
     | field "any(i['id'] == '${EVENT_ID}' for i in d)")"
assert 'Etkinlik silinir' '204' "$(status DELETE "/api/calendar/events/${EVENT_ID}" "$ADMIN_TOKEN")"

# --------------------------------------------------------------- Duyurular
section '14. Duyurular'

BODY_ANN='{"title":"Stüdyo toplantısı Cuma","content":"Tüm ekipler saat 15:00 katılacak.","priority":2,"isPinned":true}'
ANN=$(request POST /api/announcements "$ADMIN_TOKEN" "$BODY_ANN")
ANN_ID=$(echo "$ANN" | field "d['id']")
assert 'Yönetici duyuru yayınlar' 'True' "$([[ -n $ANN_ID ]] && echo True || echo False)"
assert 'Sıradan üye duyuru yayınlayamaz' '403' \
  "$(status POST /api/announcements "$OUTSIDER_TOKEN" "$BODY_ANN")"
assert 'Duyuru herkese görünür' 'True' \
  "$(request GET /api/announcements "$OUTSIDER_TOKEN" | field "any(a['id']=='${ANN_ID}' for a in d)")"
assert 'Sabitlenen duyuru en üstte' "$ANN_ID" "$(request GET /api/announcements "$ADMIN_TOKEN" | field "d[0]['id']")"
assert 'Duyuru bildirimi üretildi' 'True' \
  "$(request GET /api/notifications "$OUTSIDER_TOKEN" | field "any(n['type']==8 for n in d['items'])")"

# --------------------------------------------------------------- Dashboard
section '15. Dashboard'

DASH=$(request GET "/api/dashboard?projectId=${PROJECT_ID}&onlyMyTasks=false" "$ADMIN_TOKEN")
assert 'Dashboard yanıt veriyor' '200' \
  "$(status GET "/api/dashboard?projectId=${PROJECT_ID}&onlyMyTasks=false" "$ADMIN_TOKEN")"
assert 'Görev sayıları hesaplandı' 'True' "$(echo "$DASH" | field "d['totalTaskCount'] > 0")"
assert 'Tamamlanma yüzdesi 0-100 arasında' 'True' \
  "$(echo "$DASH" | field "0 <= d['completionPercent'] <= 100")"
assert 'Gecikmiş görevler kartı dolu' 'True' "$(echo "$DASH" | field "len(d['overdueTasks']) >= 1")"
assert 'Son aktiviteler dolu' 'True' "$(echo "$DASH" | field "len(d['recentActivities']) > 0")"
assert 'Duyurular kartı dolu' 'True' "$(echo "$DASH" | field "len(d['announcements']) >= 1")"
assert 'Yaklaşan toplantı kartı dolu' 'True' "$(echo "$DASH" | field "len(d['upcomingMeetings']) >= 1")"

# ---------------------------------------------------------------- Raporlar
section '16. Raporlama'

REPORTS=$(request GET "/api/reports?projectId=${PROJECT_ID}" "$ADMIN_TOKEN")
assert 'Durum dağılımı 7 kolon' '7' "$(echo "$REPORTS" | field "len(d['statusDistribution'])")"
assert 'Öncelik dağılımı 5 seviye' '5' "$(echo "$REPORTS" | field "len(d['priorityDistribution'])")"
assert 'Haftalık seri 12 nokta' '12' "$(echo "$REPORTS" | field "len(d['weeklyCompleted'])")"
assert 'Aylık seri 12 nokta' '12' "$(echo "$REPORTS" | field "len(d['monthlyCompleted'])")"
assert 'Sprint başarı serisi dolu' 'True' "$(echo "$REPORTS" | field "len(d['sprintSuccess']) >= 1")"
assert 'Takım performansı dolu' 'True' "$(echo "$REPORTS" | field "len(d['teamPerformance']) >= 1")"
assert 'Kullanıcı performansı dolu' 'True' "$(echo "$REPORTS" | field "len(d['userPerformance']) >= 1")"
assert 'Durum grafiği renk taşır' 'True' \
  "$(echo "$REPORTS" | field "all(p['colorHex'] for p in d['statusDistribution'])")"

# ------------------------------------------------------------------ Arama
section '17. Global arama'

assert 'Kısa sorgu boş sonuç döner' '0' \
  "$(request GET "/api/search?query=a" "$ADMIN_TOKEN" | field "d['totalCount']")"
SEARCH=$(request GET "/api/search?query=${PROJECT_KEY}" "$ADMIN_TOKEN")
assert 'Proje anahtarıyla arama bulur' 'True' "$(echo "$SEARCH" | field "len(d['projects']) >= 1")"
assert 'Görev anahtarıyla arama bulur' 'True' \
  "$(request GET "/api/search?query=${TASK_KEY}" "$ADMIN_TOKEN" | field "len(d['tasks']) >= 1")"
assert 'Kullanıcı adıyla arama bulur' 'True' \
  "$(request GET "/api/search?query=Duman" "$ADMIN_TOKEN" | field "len(d['users']) >= 1")"
assert 'Proje dışı kullanıcı görev bulamaz' '0' \
  "$(request GET "/api/search?query=${TASK_KEY}" "$OUTSIDER_TOKEN" | field "len(d['tasks'])")"

# ---------------------------------------------------------------- SignalR
section '18. SignalR erişim noktaları'

assert 'Sohbet hub negotiate tokensız reddedilir' '401' \
  "$(status POST /hubs/chat/negotiate)"
assert 'Sohbet hub negotiate tokenla kabul edilir' '200' \
  "$(status POST "/hubs/chat/negotiate?negotiateVersion=1&access_token=${ADMIN_TOKEN}")"
assert 'Presence hub negotiate tokenla kabul edilir' '200' \
  "$(status POST "/hubs/presence/negotiate?negotiateVersion=1&access_token=${ADMIN_TOKEN}")"

# ------------------------------------------------------- Mantıksal silme
section '19. Mantıksal silme (soft delete)'

assert 'Takım silinir' '204' "$(status DELETE "/api/teams/${TEAM_ID}" "$ADMIN_TOKEN")"
assert 'Silinen takım listede görünmez' '0' \
  "$(request GET "/api/teams?search=Duman%20Testi%20${SUFFIX}" "$ADMIN_TOKEN" | field 'len(d)')"
assert 'Silinen takıma erişilemez' '404' "$(status GET "/api/teams/${TEAM_ID}" "$ADMIN_TOKEN")"
assert 'Proje silinir' '204' "$(status DELETE "/api/projects/${PROJECT_ID}" "$ADMIN_TOKEN")"
assert 'Silinen projeye erişilemez' '404' "$(status GET "/api/projects/${PROJECT_ID}" "$ADMIN_TOKEN")"
assert 'Kullanıcı silinir' '204' "$(status DELETE "/api/users/${TEST_USER_ID}" "$ADMIN_TOKEN")"
assert 'Proje dışı test kullanıcısı silinir' '204' \
  "$(status DELETE "/api/users/${OUTSIDER_ID}" "$ADMIN_TOKEN")"
assert 'Silinen kullanıcı giriş yapamaz' '401' \
  "$(status POST /api/auth/login '' "$BODY_MEMBER_LOGIN")"

# ---------------------------------------------------------------- Sonuç
printf '\n%s%d test geçti%s' "$GREEN" "$PASSED" "$RESET"

if ((FAILED > 0)); then
  printf ', %s%d test başarısız%s\n' "$RED" "$FAILED" "$RESET"
  exit 1
fi

printf ', başarısız yok.%s\n' "$RESET"
printf '%sNot: Test kayıtları mantıksal olarak silindi. Veritabanından tamamen%s\n' "$DIM" "$RESET"
printf '%stemizlemek için: scripts/clean-test-data.sql%s\n' "$DIM" "$RESET"
