#!/usr/bin/env bash
#
# Arayüzü derleyip GitHub Pages'e (gh-pages dalına) yayınlar.
#
# Kullanım:
#   VITE_API_BASE_URL=https://api-adresiniz ./scripts/deploy-pages.sh
#
# Neden Actions değil de bu script: workflow dosyası göndermek, kullanılan
# kişisel erişim token'ında 'workflow' izni ister. Bu yol yalnızca normal
# push yetkisiyle çalışır. Token izni açılırsa bir Actions workflow'una
# geçilebilir; Pages ayarını "Deploy from a branch" bırakmak yeterlidir.

set -euo pipefail

cd "$(dirname "$0")/.."

REPO_URL=${REPO_URL:-$(git -C .. remote get-url origin)}
# Pages projeyi /<depo-adi>/ altında sunar; taban yol buna göre kurulur.
REPO_NAME=$(basename "${REPO_URL%.git}")
BASE_PATH=${VITE_BASE_PATH:-/$REPO_NAME/}

if [ -z "${VITE_API_BASE_URL:-}" ]; then
  printf 'HATA: VITE_API_BASE_URL tanımlı değil.\n' >&2
  printf 'Bu değer derleme anında pakete gömülür; sonradan değiştirilemez.\n' >&2
  printf 'Örnek: VITE_API_BASE_URL=https://gameflow-api.onrender.com %s\n' "$0" >&2
  exit 1
fi

printf 'Derleniyor  → taban yol %s, API %s\n' "$BASE_PATH" "$VITE_API_BASE_URL"
VITE_BASE_PATH="$BASE_PATH" VITE_API_BASE_URL="$VITE_API_BASE_URL" npm run build

# Pages istemci tarafı yönlendirmeyi bilmez: /projeler gibi bir adrese doğrudan
# girildiğinde 404.html sunulur. index.html'in kopyalanması uygulamanın yüklenip
# rotayı çözmesini sağlar (durum kodu 404 kalır, içerik doğrudur).
cp dist/index.html dist/404.html

# Jekyll, adı _ ile başlayan dosyaları yok sayar; bu dosya onu devre dışı bırakır.
touch dist/.nojekyll

WORKTREE=$(mktemp -d)
trap 'rm -rf "$WORKTREE"' EXIT

cp -R dist/. "$WORKTREE/"

git -C "$WORKTREE" init -q -b gh-pages
git -C "$WORKTREE" add -A
git -C "$WORKTREE" commit -q -m "GameFlow arayüzü — $(date +%Y-%m-%d\ %H:%M)"
git -C "$WORKTREE" remote add origin "$REPO_URL"

# gh-pages yalnızca derleme çıktısını taşır, geçmişi anlamlı değildir;
# her yayında baştan yazılır.
git -C "$WORKTREE" push -q --force origin gh-pages

printf 'Yayınlandı  → https://%s.github.io/%s/\n' \
  "$(basename "$(dirname "${REPO_URL%.git}")" | tr '[:upper:]' '[:lower:]')" "$REPO_NAME"
