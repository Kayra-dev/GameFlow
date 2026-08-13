#!/usr/bin/env bash
#
# GameFlow'u tek komutla ayağa kaldırır.
#
# Sırayla: PostgreSQL kontrolü → backend (5080) → frontend (5173).
# Ctrl+C ile her ikisi birlikte kapanır.
#
# Kullanım:
#   ./baslat.sh
#
set -uo pipefail

cd "$(dirname "$0")"

DB_PORT=5434
API_PORT=5080
WEB_PORT=5173

if [[ -t 1 ]]; then
  GREEN=$'\033[32m'; RED=$'\033[31m'; YELLOW=$'\033[33m'
  DIM=$'\033[2m'; BOLD=$'\033[1m'; RESET=$'\033[0m'
else
  GREEN=''; RED=''; YELLOW=''; DIM=''; BOLD=''; RESET=''
fi

info() { printf '%s▸%s %s\n' "$BOLD" "$RESET" "$1"; }
ok()   { printf '  %s✓%s %s\n' "$GREEN" "$RESET" "$1"; }
warn() { printf '  %s!%s %s\n' "$YELLOW" "$RESET" "$1"; }
die()  { printf '  %s✗%s %s\n' "$RED" "$RESET" "$1"; exit 1; }

# Kapanışta başlattığımız süreçler birlikte sonlandırılır.
API_PID=''
WEB_PID=''

cleanup() {
  printf '\n'
  info 'Kapatılıyor…'
  [[ -n $API_PID ]] && kill "$API_PID" 2>/dev/null
  [[ -n $WEB_PID ]] && kill "$WEB_PID" 2>/dev/null
  wait 2>/dev/null
  ok 'Sunucular kapatıldı.'
}
trap cleanup EXIT INT TERM

# Portu kullanan eski bir süreç varsa kullanıcıyı uyar; sessizce öldürmeyiz.
require_free_port() {
  local port=$1 name=$2

  if curl -sf --max-time 1 "http://localhost:${port}" -o /dev/null 2>/dev/null \
     || nc -z localhost "$port" 2>/dev/null; then
    die "$name için ${port} portu meşgul. Önceki süreci kapatın:
      lsof -nP -iTCP:${port} -sTCP:LISTEN
      kill <PID>"
  fi
}

printf '%sGameFlow başlatılıyor%s\n\n' "$BOLD" "$RESET"

# ---------------------------------------------------------------- 1. Veritabanı
info "1/3 PostgreSQL (port ${DB_PORT})"

if ! command -v pg_isready > /dev/null; then
  die 'pg_isready bulunamadı. PostgreSQL kurulu değil gibi görünüyor.'
fi

if ! pg_isready -h localhost -p "$DB_PORT" -t 3 > /dev/null 2>&1; then
  warn "Veritabanı yanıt vermiyor, başlatılıyor…"
  brew services start postgresql@17 > /dev/null 2>&1 || true
  sleep 4
fi

pg_isready -h localhost -p "$DB_PORT" -t 3 > /dev/null 2>&1 \
  || die "PostgreSQL ${DB_PORT} portunda ayağa kalkmadı.
      Elle başlatmayı deneyin: brew services start postgresql@17"

ok "Veritabanı hazır."

# ------------------------------------------------------------------- 2. Backend
info "2/3 Backend API (port ${API_PORT})"

require_free_port "$API_PORT" 'Backend'

(
  cd backend/src/GameFlow.Api
  ASPNETCORE_ENVIRONMENT=Development dotnet run --no-launch-profile \
    --urls "http://localhost:${API_PORT}"
) > /tmp/gameflow-api.log 2>&1 &
API_PID=$!

for _ in $(seq 1 40); do
  if curl -sf --max-time 1 "http://localhost:${API_PORT}/health" -o /dev/null; then
    break
  fi

  # Süreç düştüyse beklemeye devam etmenin anlamı yok.
  if ! kill -0 "$API_PID" 2>/dev/null; then
    printf '\n%s--- API kayıtlarının sonu ---%s\n' "$DIM" "$RESET"
    tail -25 /tmp/gameflow-api.log
    die 'Backend başlatılamadı (kayıtlar yukarıda).'
  fi

  sleep 1
done

curl -sf --max-time 2 "http://localhost:${API_PORT}/health" -o /dev/null \
  || die "Backend ${API_PORT} portunda yanıt vermiyor. Kayıtlar: /tmp/gameflow-api.log"

ok "API çalışıyor · kayıtlar: /tmp/gameflow-api.log"

# ------------------------------------------------------------------ 3. Frontend
info "3/3 Frontend (port ${WEB_PORT})"

require_free_port "$WEB_PORT" 'Frontend'

if [[ ! -d frontend/node_modules ]]; then
  warn 'Bağımlılıklar kurulu değil, npm install çalıştırılıyor…'
  (cd frontend && npm install) || die 'npm install başarısız oldu.'
fi

(cd frontend && npm run dev) > /tmp/gameflow-web.log 2>&1 &
WEB_PID=$!

for _ in $(seq 1 30); do
  if curl -sf --max-time 1 "http://localhost:${WEB_PORT}" -o /dev/null; then
    break
  fi

  if ! kill -0 "$WEB_PID" 2>/dev/null; then
    printf '\n%s--- Frontend kayıtlarının sonu ---%s\n' "$DIM" "$RESET"
    tail -25 /tmp/gameflow-web.log
    die 'Frontend başlatılamadı (kayıtlar yukarıda).'
  fi

  sleep 1
done

ok "Arayüz çalışıyor · kayıtlar: /tmp/gameflow-web.log"

cat <<BANNER

${BOLD}Hazır.${RESET}

  Arayüz          ${GREEN}http://localhost:${WEB_PORT}${RESET}
  API dokümanı    ${DIM}http://localhost:${API_PORT}/docs${RESET}

  Giriş           ${BOLD}admin@gameflow.dev${RESET}  /  ${BOLD}Admin!2345${RESET}

${DIM}Kapatmak için Ctrl+C.${RESET}

BANNER

# Süreçlerden biri düşerse betik de sonlanır ve trap temizliği yapar.
wait -n "$API_PID" "$WEB_PID"
