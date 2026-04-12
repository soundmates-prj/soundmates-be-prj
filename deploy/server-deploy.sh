#!/usr/bin/env bash
# ===========================================================
# Soundmates BE - Safe Deploy Script
# Chỉ rebuild & recreate app services, KHÔNG đụng đến DB
# ===========================================================
set -euo pipefail

APP_DIR="${APP_DIR:-/opt/soundmates-be/soundmates-be-prj}"
BRANCH="${BRANCH:-duc}"

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
echo "=== [2b/6] Đảm bảo external volumes tồn tại (tạo nếu chưa có) ==="
for vol in soundmates_postgres_data soundmates_mongodb_data soundmates_rabbitmq_data soundmates_ai_audios_data; do
  if ! docker volume ls -q | grep -q "^${vol}$"; then
    echo "  Tạo volume: $vol"
    docker volume create "$vol"
  else
    echo "  ✓ Volume $vol đã tồn tại"
  fi
done

echo ""
echo "=== [3/6] Chuyển containers sang project 'soundmates' (xử lý một lần) ==="
# Nếu container đang chạy nhưng KHÔNG thuộc project 'soundmates' hiện tại,
# cần stop + remove container (KHÔNG xóa volume) để project mới tiếp quản
ALL_CONTAINERS=(
  soundmates-postgres soundmates-mongodb soundmates-rabbitmq
  soundmates-auth-service soundmates-auth-query-service
  soundmates-live-session-service soundmates-account-content-service
  soundmates-ai-service soundmates-api-gateway
)

for c in "${ALL_CONTAINERS[@]}"; do
  if docker ps -a --format '{{.Names}}' | grep -q "^${c}$"; then
    # Kiểm tra xem container có thuộc project 'soundmates' không
    PROJECT=$(docker inspect "$c" --format '{{index .Config.Labels "com.docker.compose.project"}}' 2>/dev/null || echo "")
    if [ "$PROJECT" != "soundmates" ]; then
      echo "  Dọn container cũ (project='$PROJECT'): $c"
      docker stop "$c" 2>/dev/null || true
      docker rm "$c" 2>/dev/null || true
    else
      echo "  ✓ Container $c đã thuộc project soundmates"
    fi
  fi
done

echo "  Khởi động DB containers (không recreate nếu đã chạy đúng project)..."
docker compose up -d postgres mongodb rabbitmq
echo "  Đợi postgres healthy..."
timeout 90 sh -c 'until docker exec soundmates-postgres pg_isready -U postgres -q; do sleep 3; done'
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
