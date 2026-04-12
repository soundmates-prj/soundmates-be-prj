#!/usr/bin/env bash
# ===========================================================
# Soundmates BE - Safe Deploy Script
# Chỉ rebuild & recreate app services, KHÔNG đụng đến DB
# ===========================================================
set -euo pipefail

APP_DIR="${APP_DIR:-/opt/soundmates-be/soundmates-be-prj}"
BRANCH="${BRANCH:-dev}"

echo "=== [1/6] Pull code từ nhánh $BRANCH ==="
cd "$APP_DIR"
git fetch origin
git checkout "$BRANCH" 2>/dev/null || git checkout -b "$BRANCH" "origin/$BRANCH"
git reset --hard "origin/$BRANCH"
echo "✓ Code: $(git log -1 --oneline)"

echo ""
echo "=== [2/6] Migrate volumes sang tên cố định (chỉ chạy 1 lần đầu) ==="

migrate_volume() {
  local OLD_PREFIX="${1}"   # e.g. soundmates-be-prj
  local NEW_NAME="${2}"     # e.g. soundmates_postgres_data
  local OLD_NAME="${OLD_PREFIX}_${3}"  # e.g. soundmates-be-prj_postgres-data

  if docker volume ls -q | grep -q "^${OLD_NAME}$"; then
    if ! docker volume ls -q | grep -q "^${NEW_NAME}$"; then
      echo "  Migrating volume: $OLD_NAME → $NEW_NAME ..."
      docker volume create "$NEW_NAME"
      docker run --rm \
        -v "${OLD_NAME}:/source:ro" \
        -v "${NEW_NAME}:/dest" \
        alpine sh -c "cd /source && cp -av . /dest/"
      echo "  ✓ Volume $NEW_NAME đã được tạo với data từ $OLD_NAME"
    else
      echo "  ✓ Volume $NEW_NAME đã tồn tại, bỏ qua migrate"
    fi
  else
    echo "  - Volume cũ $OLD_NAME không tồn tại, skip (volume mới sẽ tự tạo)"
  fi
}

# Thử migrate từ cả hai project name phổ biến
for OLD_PREFIX in "soundmates-be-prj" "soundmates"; do
  migrate_volume "$OLD_PREFIX" "soundmates_postgres_data"  "postgres-data"
  migrate_volume "$OLD_PREFIX" "soundmates_mongodb_data"   "mongodb-data"
  migrate_volume "$OLD_PREFIX" "soundmates_rabbitmq_data"  "rabbitmq-data"
  migrate_volume "$OLD_PREFIX" "soundmates_ai_audios_data" "ai-audios-data"
done

echo ""
echo "=== [3/6] Đảm bảo DB containers đang chạy (không recreate) ==="
docker compose up -d --no-recreate postgres mongodb rabbitmq
echo "  Đợi postgres healthy..."
timeout 60 sh -c 'until docker exec soundmates-postgres pg_isready -U postgres -q; do sleep 2; done'
echo "  ✓ Postgres healthy"

echo ""
echo "=== [4/6] Build lại chỉ các app service images ==="
docker compose build \
  auth-service \
  auth-query-service \
  live-session-service \
  account-content-service \
  ai-service \
  api-gateway

echo ""
echo "=== [5/6] Recreate chỉ app services (KHÔNG đụng DB) ==="
docker compose up -d --no-deps --force-recreate \
  auth-service \
  auth-query-service \
  live-session-service \
  account-content-service \
  ai-service \
  api-gateway

echo ""
echo "=== [6/6] Kiểm tra trạng thái ==="
sleep 5
docker compose ps
echo ""
echo "=== Log 30 dòng cuối api-gateway ==="
docker compose logs --tail=30 api-gateway

echo ""
echo "✅ Deploy hoàn tất! DB containers không bị đụng vào."
