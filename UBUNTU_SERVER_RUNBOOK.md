# So tay van hanh server Ubuntu - Soundmates

Tai lieu nay tong hop cac duong dan va lenh su dung thuong xuyen tren VPS Ubuntu.

## 1) Duong dan quan trong tren VPS

- Backend source: `/opt/soundmates-be/soundmates-be-prj`
- Backend env: `/opt/soundmates-be/soundmates-be-prj/.env`
- Backend deploy script: `/opt/soundmates-be/soundmates-be-prj/deploy/server-deploy.sh`
- Frontend source: `/opt/soundmates-web/soundmates-web-prj`
- Frontend env: `/opt/soundmates-web/soundmates-web-prj/.env`
- Nginx site config: `/etc/nginx/sites-available/soundmates-web`
- Nginx site enable symlink: `/etc/nginx/sites-enabled/soundmates-web`
- AzuraCast root: `/var/azuracast`
- AzuraCast env: `/var/azuracast/.env`
- AzuraCast utility script: `/var/azuracast/docker.sh`
- VieNeu source (thuong gap): `/opt/vieneu-tts/soundmates-vieneu-tts`
- VieNeu service file (neu dung systemd): `/etc/systemd/system/vieneu.service`

## 2) Kiem tra nhanh he thong
ssh root@161.97.85.232
```bash
hostname
whoami
pwd
uptime
free -h
df -h
```

## 3) Backend (BE) - pull nhanh dev va deploy

> ⚠️ CANH BAO: KHONG dung `docker compose up -d --build` truc tiep!
> Lenh nay co the recreate postgres container va mat data.
> LUON dung script deploy ben duoi.

```bash
cd /opt/soundmates-be/soundmates-be-prj

git fetch origin
git checkout dev || git checkout -b dev origin/dev
git reset --hard origin/dev
bash ./deploy/server-deploy.sh
# Kiem tra nhanh branch + commit
git branch --show-current
git log -1 --oneline

# ✅ Deploy AN TOAN - chi rebuild app services, GIU NGUYEN DB
APP_DIR=/opt/soundmates-be/soundmates-be-prj BRANCH=dev bash ./deploy/server-deploy.sh
```


### Neu can deploy mot service rieng le (khong build lai tat ca)

```bash
cd /opt/soundmates-be/soundmates-be-prj

# Build va recreate chi 1 service
docker compose build <ten-service>
docker compose up -d --no-deps --force-recreate <ten-service>

# Vi du: chi deploy lai api-gateway
docker compose build api-gateway
docker compose up -d --no-deps --force-recreate api-gateway
```

### Neu can restart tat ca app service (KHONG rebuild - giu nguyen image)

```bash
cd /opt/soundmates-be/soundmates-be-prj

# Chi restart, khong recreate
docker compose restart auth-service auth-query-service live-session-service account-content-service ai-service api-gateway

# Backfill toàn bộ users từ Postgres vào MongoDB
docker exec soundmates-postgres psql -U postgres -d auth_db -c "
INSERT INTO outbox_messages (id, type, payload, occurred_on_utc, processed_on_utc, error)
SELECT 
  uuid_generate_v4(),
  'auth.user.updated',
  json_build_object(
    'id',             u.id::text,
    'username',       coalesce(u.username, ''),
    'email',          coalesce(u.email, ''),
    'firstName',      coalesce(u.first_name, ''),
    'lastName',       coalesce(u.last_name, ''),
    'roleId',         case when u.role_id is null then null else u.role_id::text end,
    'roleName',       coalesce(ur.name, 'User'),
    'isActive',       coalesce(u.is_active, true),
    'isVerified',     (u.email_verified_at is not null),
    'emailVerifiedAt', u.email_verified_at,
    'profileImageUrl', p.profile_image_url,
    'bio',            p.bio
  )::text,
  now() at time zone 'utc',
  null, null
FROM users u
LEFT JOIN user_roles ur ON ur.id = u.role_id
LEFT JOIN profiles p ON p.user_id = u.id;
"

sleep 20

docker exec soundmates-mongodb mongosh "mongodb://localhost:27017/auth_query" --quiet \
  --eval "printjson({ users_read: db.users_read.countDocuments() })"

```



### Log BE

```bash
cd /opt/soundmates-be/soundmates-be-prj
docker compose ps
docker compose logs --tail=120 api-gateway
docker compose logs --tail=120 auth-service
docker compose logs --tail=120 auth-query-service
docker compose logs --tail=120 account-content-service
docker compose logs --tail=120 live-session-service
docker compose logs --tail=120 ai-service
```

## 4) Frontend (Web) - pull, build, reload Nginx

```bash
cd /opt/soundmates-web/soundmates-web-prj

git fetch origin
git checkout dev || git checkout -b dev origin/dev
git reset --hard origin/dev

# Neu Vite bao Node cu, nang cap Node 22
node -v || true

npm ci
npm run build

# Reload web
systemctl reload nginx
systemctl status nginx --no-pager
```

### Nginx log

```bash
tail -n 200 /var/log/nginx/access.log
tail -n 200 /var/log/nginx/error.log
```

## 5) VieNeu TTS

### Start nhanh VieNeu tren VPS

```bash
# Cach 1: Neu da co service systemd
sudo systemctl daemon-reload
sudo systemctl enable vieneu
sudo systemctl restart vieneu
sudo systemctl status vieneu --no-pager
curl -f http://127.0.0.1:8008/models

# Cach 2: Chay nen bang nohup (khi chua dung systemd)
cd /opt/vieneu-tts/soundmates-vieneu-tts
source .venv/bin/activate
nohup python3 apps/web_stream.py > /var/log/vieneu.log 2>&1 &
disown

# Kiem tra cong 8008 da mo
ss -ltnp | grep 8008 || true
curl -f http://127.0.0.1:8008/models
```

### Neu chay bang systemd

```bash
systemctl start vieneu
systemctl status vieneu --no-pager
journalctl -u vieneu -n 200 --no-pager
journalctl -u vieneu -f
```

### Neu chay bang nohup

```bash
tail -n 200 /var/log/vieneu.log
tail -f /var/log/vieneu.log
```

### Test endpoint VieNeu

```bash
curl -f http://127.0.0.1:8008/models
```

## 6) AzuraCast

```bash
cd /var/azuracast
docker compose ps
docker compose logs --tail=120 web
./docker.sh update
```

### Kiem tra API AzuraCast

```bash
curl -f http://127.0.0.1:5000/api/status
curl -f -H "X-API-Key: YOUR_API_KEY" http://127.0.0.1:5000/api/stations
```

## 7) Lenh chinh sua .env an toan

```bash
cd /opt/soundmates-be/soundmates-be-prj
cp .env ".env.bak.$(date +%F-%H%M%S)"

# Vi du: cap nhat cac bien thanh toan/web
sed -i 's#^FRONTEND_URL=.*#FRONTEND_URL=http://161.97.85.232#' .env
sed -i 's#^VNPAY_RETURN_URL=.*#VNPAY_RETURN_URL=http://161.97.85.232:8080/api/v1/payments/vnpay/callback#' .env
sed -i 's#^PAY_OS_RETURN_URL=.*#PAY_OS_RETURN_URL=http://161.97.85.232:8080/api/v1/payments/payos/return#' .env

# Kiem tra bien sau khi sua
grep -E '^(FRONTEND_URL|VNPAY_RETURN_URL|PAY_OS_RETURN_URL|CORS_ALLOWED_ORIGINS)=' .env
```

## 8) Kiem tra CORS nhanh (gateway)

```bash
curl -i -X OPTIONS "http://127.0.0.1:8080/api/v1/posts/published?page=1&pageSize=12" \
  -H "Origin: http://161.97.85.232" \
  -H "Access-Control-Request-Method: GET"
```

## 9) Kiem tra tai nguyen Docker

```bash
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
docker stats --no-stream
docker image ls
docker volume ls
```

## 10) Tang timeout TTS len 600s de xac minh nghen xu ly

```bash
cd /opt/soundmates-be/soundmates-be-prj

# Sao luu env truoc khi sua
cp .env ".env.bak.$(date +%F-%H%M%S)"

# Tang timeout TTS len 600s
sed -i 's/^TTS_TIMEOUT_SECONDS=.*/TTS_TIMEOUT_SECONDS=600/' .env

# Kiem tra gia tri trong file env
grep -n '^TTS_TIMEOUT_SECONDS=' .env

# Recreate rieng ai-service de nhan env moi
docker compose --env-file .env up -d --build --force-recreate --no-deps ai-service

# Kiem tra env thuc te trong container
docker exec soundmates-ai-service sh -lc 'printenv | grep -E "Tts__TimeoutSeconds|TTS_TIMEOUT_SECONDS|Tts__BaseUrl|Tts__SynthesizePath"'

# Theo doi log de xac nhan het loi timeout 100s
docker logs --tail 120 -f soundmates-ai-service
```

Neu van timeout voi 600s, uu tien kiem tra toc do phan hoi VieNeu va tai nguyen CPU/RAM tren VPS.

### Neu log van bao HttpClient.Timeout 100 seconds

```bash
cd /opt/soundmates-be/soundmates-be-prj

# 1) Xac minh container hien tai duoc tao tu thu muc nao
docker inspect soundmates-ai-service --format '{{ index .Config.Labels "com.docker.compose.project.working_dir" }}'

# 2) Xac minh env that su dang co trong container
docker inspect soundmates-ai-service --format '{{range .Config.Env}}{{println .}}{{end}}' | grep -E '^Tts__TimeoutSeconds=|^TTS_TIMEOUT_SECONDS='

# 3) Xac minh compose da resolve ra 600 truoc khi recreate
docker compose --env-file .env config | sed -n '/ai-service:/,/^[^ ]/p' | grep -E 'Tts__TimeoutSeconds|TTS_TIMEOUT_SECONDS'

# 4) Recreate lai dung service + xem startup log timeout
docker compose --env-file .env up -d --build --force-recreate --no-deps ai-service
docker logs --tail 80 soundmates-ai-service | grep -E 'Configured VieNeu HttpClient|TimeoutSeconds|HttpClient.Timeout'
```

Neu buoc 2 van rong hoac khong ra 600, thuong la dang sua nham repo path hoac chay docker compose o thu muc khac.

### Neu env da la 600 nhung log van timeout 100s

```bash
cd /opt/soundmates-be/soundmates-be-prj

# 1) Sua dung file DI theo path day du (khong dung regex perl phuc tap)
FILE_DI=services/ai-service/AiService.Infrastructure/DependencyInjection.cs
sed -i 's/AddHttpClient<VieNeuTtsClient>((sp, http) =>/AddHttpClient<ITtsClient, VieNeuTtsClient>((sp, http) =>/' "$FILE_DI"
sed -i '/AddScoped<ITtsClient, VieNeuTtsClient>();/d' "$FILE_DI"
grep -n 'AddHttpClient<ITtsClient, VieNeuTtsClient>\|AddScoped<ITtsClient, VieNeuTtsClient>' "$FILE_DI"

# 2) Bo sung thu vien he thong cho ai-service image
FILE_DOCKER=services/ai-service/AiService.Api/Dockerfile
perl -0777 -i -pe 's#FROM base AS final\nWORKDIR /app\n#FROM base AS final\nWORKDIR /app\nRUN apt-get update \\\n    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \\\n    && rm -rf /var/lib/apt/lists/*\n#s unless /libgssapi-krb5-2/' "$FILE_DOCKER"
grep -n 'libgssapi-krb5-2' "$FILE_DOCKER"

# 3) Rebuild + recreate ai-service
docker compose build --no-cache ai-service
docker compose --env-file .env up -d --force-recreate --no-deps ai-service

# 4) Verify sau khi len container moi
docker inspect soundmates-ai-service --format '{{range .Config.Env}}{{println .}}{{end}}' | grep -E '^Tts__TimeoutSeconds=|^TTS_TIMEOUT_SECONDS='
docker logs --since 5m soundmates-ai-service | grep -E 'Configured VieNeu HttpClient|HttpClient.Timeout|TryCallStreamPostAsync|Cannot load library'
```

## 11) Gemini 404 - Tim model ho tro generateContent

```bash
cd /opt/soundmates-be/soundmates-be-prj

# Lay API key tu env hien tai
GEMINI_KEY=$(grep '^LLM_API_KEY=' .env | cut -d '=' -f2-)

# Liet ke model co ho tro generateContent (can jq)
curl -s "https://generativelanguage.googleapis.com/v1beta/models?key=${GEMINI_KEY}" \
| jq -r '.models[] | select((.supportedGenerationMethods // []) | index("generateContent")) | .name'

# Vi du doi model sang gemini-2.5-flash
sed -i 's#^LLM_MODEL=.*#LLM_MODEL=gemini-2.5-flash#' .env
grep -n '^LLM_MODEL=' .env

# Recreate ai-service de nhan model moi
docker compose --env-file .env up -d --build --force-recreate --no-deps ai-service

# Kiem tra log goi Gemini
docker logs --tail 120 soundmates-ai-service | grep -E 'Calling Gemini API model|Gemini API error'
```

Neu VPS chua co jq:

```bash
apt-get update && apt-get install -y jq
```

## 12) Gemini 503 - Model dung nhung provider qua tai

```bash
cd /opt/soundmates-be/soundmates-be-prj
GEMINI_KEY=$(grep '^LLM_API_KEY=' .env | cut -d '=' -f2-)

# Test truc tiep Gemini tu VPS (khong qua app)
curl -s -X POST "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=${GEMINI_KEY}" \
  -H "Content-Type: application/json" \
  -d '{"contents":[{"parts":[{"text":"Xin chao"}]}]}'

# Neu hay gap 503, doi sang model lite de giam nghen
sed -i 's#^LLM_MODEL=.*#LLM_MODEL=gemini-2.0-flash-lite-001#' .env
grep -n '^LLM_MODEL=' .env

docker compose --env-file .env up -d --build --force-recreate --no-deps ai-service
docker logs --tail 120 soundmates-ai-service | grep -E 'Calling Gemini API model|Gemini API error|Transient Gemini error'
```

## 13) Trinh tu cap nhat khuyen nghi moi lan deploy

1. Pull BE nhanh `dev` + deploy script.
2. Kiem tra log `api-gateway` va `account-content-service`.
3. Pull Web nhanh `dev` + build + reload Nginx.
4. Test web route: `http://161.97.85.232`.
5. Test callback route thanh toan.
6. Neu co van de audio/live, kiem tra log VieNeu va AzuraCast.

## 14) Loi "TaskCanceledException" tai NpgsqlConnector.ConnectAsync (ai-service)

Loi nay thuong xuat hien khi request goc da bi cancel (vi du TTS timeout/connection timeout), sau do code co gang luu trang thai `failed` vao DB bang chinh cancellation token da bi huy.

### Kiem tra nhanh de tach nguyen nhan goc

```bash
cd /opt/soundmates-be/soundmates-be-prj

# 1) Xem log nguyen nhan truoc do (TTS/network) gan thoi diem loi Npgsql
docker logs --since 15m soundmates-ai-service \
  | grep -E 'TryCallStreamPostAsync|Connection timed out|TaskCanceledException|NpgsqlConnector.ConnectAsync'

# 2) Kiem tra postgres container co healthy khong
docker compose ps postgres
docker logs --tail=120 soundmates-postgres
docker exec soundmates-postgres pg_isready -U postgres

# 3) Kiem tra ket noi toi postgres tu cung network voi ai-service
NET=$(docker inspect soundmates-ai-service --format '{{range $k, $v := .NetworkSettings.Networks}}{{println $k}}{{end}}' | head -n 1)
docker run --rm --network "$NET" postgres:16-alpine \
  sh -lc 'pg_isready -h postgres -p 5432 -U postgres'
```

Neu buoc 2-3 OK, kha nang cao loi Npgsql o tren la loi phu do token da bi cancel, can xem dong loi truoc do trong log ai-service de xu ly nguyen nhan goc (thuong la duong mang ai-service -> VieNeu).

### Cap nhat ai-service de khong che mat loi goc

```bash
cd /opt/soundmates-be/soundmates-be-prj

# Build lai ai-service voi patch moi
docker compose build --no-cache ai-service
docker compose --env-file .env up -d --force-recreate --no-deps ai-service

# Theo doi log sau khi cap nhat
docker logs --since 10m -f soundmates-ai-service
```

## 15) Neu log da nhan `200` tu VieNeu nhung van `The operation was canceled`

Mau log thuong gap:
- `Received HTTP response headers ... - 200`
- Sau do loi trong `ChunkedEncodingReadStream` / `ReadAsByteArrayAsync`

Day thuong la request da bi huy tu upstream (gateway/nginx/client timeout) trong luc ai-service dang doc stream audio.

### Checklist nhanh

```bash
cd /opt/soundmates-be/soundmates-be-prj

# 1) Xem route timeout hien tai cua gateway cho nhom AI
grep -n '"UpstreamPathTemplate": "/api/v1/scripts\|"UpstreamPathTemplate": "/api/v1/audios\|"TimeoutValue"' api-gateway/ocelot.json | sed -n '1,120p'

# 2) Rebuild/recreate gateway sau khi cap nhat timeout
docker compose build api-gateway
docker compose up -d --force-recreate --no-deps api-gateway

# 3) Theo doi log gateway va ai-service trong 1 lan goi generate
docker logs --since 10m -f soundmates-api-gateway
docker logs --since 10m -f soundmates-ai-service
```

Khuyen nghi: timeout route AI tren Ocelot phai lon hon timeout TTS noi bo.
- Vi du: `TTS_TIMEOUT_SECONDS=600` thi `QoSOptions.TimeoutValue` nen >= `700000` ms.

Neu goi API qua Nginx reverse proxy, dam bao Nginx timeout khong ngan hon Ocelot:

```nginx
proxy_read_timeout 700s;
proxy_send_timeout 700s;
send_timeout 700s;
```

### Fix nhanh 503 theo dung mau log tren

```bash
cd /opt/soundmates-be/soundmates-be-prj

# A) Xac minh gateway dang chay timeout moi (700000)
docker exec soundmates-api-gateway sh -lc "grep -n 'api/v1/audios\|api/v1/scripts\|TimeoutValue' /app/ocelot.json | sed -n '1,220p'"

# B) Recreate lai gateway de chac chan load ocelot.json moi
docker compose build api-gateway
docker compose up -d --force-recreate --no-deps api-gateway

# C) Neu di qua Nginx domain, ap timeout 700s va reload
grep -n 'proxy_read_timeout\|proxy_send_timeout\|send_timeout' /etc/nginx/sites-available/soundmates-web || true

# Neu chua co thi them vao location proxy API (chinh file theo cau hinh hien tai)
# sau do:
nginx -t && systemctl reload nginx

# D) Theo doi dong thoi 2 log trong 1 lan tao audio
docker logs --since 10m -f soundmates-api-gateway
docker logs --since 10m -f soundmates-ai-service
```

Neu van loi, mo them log VieNeu cung thoi diem de bat su kien stream bi ket thuc som:

```bash
journalctl -u vieneu -n 200 --no-pager
journalctl -u vieneu -f
```

## 16) AzuraCast `/listen/.../radio.mp3` bi Bad Request

Loi nay thuong do URL stream bi hard-code sai `station shortcode` (vi du `soundmates_station_1`) hoac dung URL cu khong trung voi cau hinh hien tai cua AzuraCast.

### Kiem tra dung shortcode va listen URL tu AzuraCast API

```bash
cd /var/azuracast

# 1) Liet ke station id + shortcode + listen_url hien tai
curl -s http://127.0.0.1:5000/api/stations \
  | jq -r '.[] | "id=\(.id) shortcode=\(.shortcode) listen=\(.listen_url)"'

# 2) Xem nowplaying station 1 (doi so 1 neu station id khac)
curl -s http://127.0.0.1:5000/api/nowplaying/1 \
  | jq -r '.station.shortcode, .station.listen_url, (.station.public_player_url // "")'
```

Sau do test dung URL tra ve tu API (khong tu go path):

```bash
LISTEN_URL=$(curl -s http://127.0.0.1:5000/api/nowplaying/1 | jq -r '.station.listen_url')
echo "$LISTEN_URL"
curl -I "$LISTEN_URL"
```

Neu URL tu API nghe OK ma URL hard-code bi 400, hay thay toan bo URL hard-code bang `listen_url` tu API.

### Kiem tra log AzuraCast khi gap 400

```bash
cd /var/azuracast
docker compose logs --tail=200 web
# AzuraCast ban nay khong co service `stations`; process station nam trong container web
docker exec azuracast supervisorctl status | grep station_ || true
```

Neu `listen_url` tra ve dang la `host.docker.internal` hoac `localhost`, can doi sang host public (IP/domain VPS) trong cau hinh AzuraCast de client ben ngoai truy cap duoc.

### Neu `api/nowplaying` tra ve `127.0.0.1:5000` gay 400

Backend da co patch uu tien URL public da sync trong DB thay vi loopback URL.

Sau khi deploy patch, chay:

```bash
cd /opt/soundmates-be/soundmates-be-prj
docker compose build --no-cache live-session-service api-gateway
docker compose --env-file .env up -d --force-recreate --no-deps live-session-service api-gateway

# Kiem tra API gateway tra nowplaying
curl -s http://127.0.0.1:8080/api/v1/station/<station_uuid>/now-playing \
  | jq -r '.data.listenUrl, .data.publicPlayerUrl'
```

Ky vong: `listenUrl` khong con la `127.0.0.1:5000/...` ma la URL public stream (thuong `http://<public-ip>:8000/listen/...`).

## 17) AzuraCast log `local_1 ... Connection failed: 404` + tra ve trang Login HTML

Mau log nay cho thay Liquidsoap dang day stream vao sai endpoint (hoac sai frontend config), nen mount `/radio.mp3` khong duoc tao.

Dau hieu:
- `station_1_backend ... Connecting mount /radio.mp3 for source@127.0.0.1`
- `Connection failed: 404, Not Found (HTTP/1.1)`
- `Error parsing XML response` va noi dung la trang `Log In - AzuraCast`

### Buoc 1: Xac minh frontend config hien tai cua station

```bash
API_KEY=$(grep '^AZURACAST_API_KEY=' /opt/soundmates-be/soundmates-be-prj/.env | cut -d '=' -f2-)

curl -s -H "X-API-Key: ${API_KEY}" http://127.0.0.1:5000/api/admin/station/1 \
  | jq -r '.backend_type, .frontend_type, .is_public, .name'
```

Ky vong: `backend_type=liquidsoap`, `frontend_type=icecast`.

### Buoc 2: Restart rieng backend/frontend cua station

```bash
curl -s -X POST -H "X-API-Key: ${API_KEY}" http://127.0.0.1:5000/api/station/1/backend/restart
curl -s -X POST -H "X-API-Key: ${API_KEY}" http://127.0.0.1:5000/api/station/1/frontend/restart
sleep 5

curl -s http://127.0.0.1:5000/api/nowplaying/1 | jq -r '.is_online, .station.listen_url'
```

Neu lenh restart frontend bao `SpawnErrorException`, chay them:

```bash
docker exec azuracast supervisorctl status | grep station_1_ || true
docker exec azuracast supervisorctl tail -100 station_1:station_1_frontend stderr || true
docker exec azuracast supervisorctl tail -100 station_1:station_1_frontend stdout || true
docker logs --tail=200 azuracast | grep -Ei 'station_1_frontend|icecast|bind|fatal|error' || true
```

### Buoc 3: Neu van 404, sua lai station profile trong AzuraCast UI

Trong AzuraCast UI:
1. Neu UI KHONG co field doi Frontend/Backend (nhu man hinh ban dang gap), bo qua buoc doi type.
2. Xac minh type qua API la du:

```bash
curl -s -H "X-API-Key: ${API_KEY}" http://127.0.0.1:5000/api/admin/station/1 \
  | jq -r '.backend_type, .frontend_type'
```

Ky vong: `liquidsoap` va `icecast`.

3. Vao menu trai `Broadcasting -> Edit Liquidsoap Configuration`:
  - Neu co custom config, tam thoi dua ve mac dinh (hoac bo cac dong `output.icecast` tu viet tay).
  - Save.
4. Vao `Broadcasting -> Mount Points`, dam bao co mount `/radio.mp3` dang enabled.
5. Bam `Restart Broadcasting`.

Neu frontend van `FATAL`, kiem tra file config sinh ra trong container:

```bash
docker exec azuracast sh -lc "grep -nE 'output\.icecast|host *=|port *=|mount *=|radio\.mp3' /var/azuracast/stations/soundmates_station_1/config/liquidsoap.liq | sed -n '1,220p'"
docker exec azuracast sh -lc "grep -nE '<listen-socket>|<port>|<bind-address>|<mount>|radio\.mp3' /var/azuracast/stations/soundmates_station_1/config/icecast.xml | sed -n '1,220p'"
```

Neu thay ca `output.icecast ... port = 5000` va `icecast.xml <port>5000</port>` thi day la case frontend dang dung cong web, rat de gay xung dot va lam frontend `FATAL`.

Luu y: `nowplaying.is_online=true` van co the xay ra trong luc frontend `FATAL` (AutoDJ/backend van chay nhung kenh phat ra ngoai chua len).

Kiem tra nhanh cong nao dang chiem 5000 trong container:

```bash
docker exec azuracast sh -lc "ss -ltnp | grep ':5000' || true"
```

Huong xu ly:
1. Vao tab tren cung `Broadcasting` (khong phai tab Administration).
2. Tim nhom `Broadcasting Ports` / `Custom Broadcasting Port` (ten co the khac tuy ban AzuraCast).
3. Dat frontend broadcast port ve `8000` (hoac de mac dinh/auto neu he thong tu cap).
4. Save, sau do `Restart Broadcasting`.

Verify lai:

```bash
docker exec azuracast supervisorctl status | grep station_1_ || true
curl -s http://127.0.0.1:5000/api/nowplaying/1 | jq -r '.is_online, .station.listen_url'
curl -I http://127.0.0.1:8000/listen/soundmates_station_1/radio.mp3 || true
```

Neu UI khong cho sua port ngay, co the hotfix tam de xac minh nguyen nhan:

```bash
docker exec azuracast sh -lc "sed -i 's/port = 5000/port = 8000/g' /var/azuracast/stations/soundmates_station_1/config/liquidsoap.liq"
docker exec azuracast sh -lc "sed -i '0,/<port>5000<\\/port>/{s//<port>8000<\\/port>/}' /var/azuracast/stations/soundmates_station_1/config/icecast.xml"
curl -s -X POST -H "X-API-Key: ${API_KEY}" http://127.0.0.1:5000/api/station/1/frontend/restart
sleep 3
docker exec azuracast supervisorctl status | grep station_1_ || true
curl -I http://127.0.0.1:8000/listen/soundmates_station_1/radio.mp3 || true
```

Hotfix nay co the bi ghi de sau khi AzuraCast regenerate config; can sua lai bang UI/API khi da xac minh duoc nguyen nhan.

Neu da hotfix nhu tren ma frontend van `BACKOFF`/`FATAL`, kiem tra ngay sau restart xem file co bi tra ve port 5000 khong:

```bash
docker exec azuracast sh -lc "grep -n 'output.icecast' /var/azuracast/stations/soundmates_station_1/config/liquidsoap.liq"
docker exec azuracast sh -lc "grep -nE '<listen-socket>|<port>|<mount-name>|<listenurl>' /var/azuracast/stations/soundmates_station_1/config/icecast.xml"
curl -s -H "X-API-Key: ${API_KEY}" http://127.0.0.1:5000/api/admin/station/1 | jq -r '.frontend_type, (.frontend_config // {})'
```

Neu output lai la `port 5000`, can sua gia tri persistent trong station settings (tab `Broadcasting`, muc `Broadcasting Ports` hoac `Custom Broadcasting Port`) ve `8000` hoac de auto/mac dinh roi `Save` + `Restart Broadcasting`.

Lenh verify sau khi sua persistent:

```bash
curl -s -X POST -H "X-API-Key: ${API_KEY}" http://127.0.0.1:5000/api/station/1/restart
sleep 5
docker exec azuracast supervisorctl status | grep station_1_ || true
curl -s http://127.0.0.1:5000/api/nowplaying/1 | jq -r '.is_online, .station.listen_url'
curl -I http://127.0.0.1:8000/listen/soundmates_station_1/radio.mp3 || true
```

### Buoc 4: Kiem tra cong stream ben ngoai VPS

```bash
ss -ltnp | grep 8000 || true
ufw status
# Neu can:
ufw allow 8000/tcp

curl -I http://127.0.0.1:8000/listen/soundmates_station_1/radio.mp3 || true
# Luu y: test public IP tren chinh VPS co the fail do hairpin NAT.
# Can test tu may ngoai VPS (laptop/4G) de xac nhan truy cap that su.
curl -I http://161.97.85.232:8000/listen/soundmates_station_1/radio.mp3 || true
```

## 18) Ubuntu BE khong sync duoc MongoDB

Luu y quan trong:
- `auth-service` dual-write sang MongoDB la non-fatal. API van co the thanh cong du MongoDB loi.
- `auth-query-service` con nghe event tu RabbitMQ de cap nhat read-side MongoDB.

### Kiem tra nhanh 4 thanh phan: mongodb, rabbitmq, auth-service, auth-query-service

```bash
cd /opt/soundmates-be/soundmates-be-prj

docker compose ps mongodb rabbitmq auth-service auth-query-service
docker compose logs --since=15m mongodb | tail -n 120
docker compose logs --since=15m rabbitmq | tail -n 120

docker compose logs --since=15m auth-service \
  | grep -Ei 'FavouriteSyncRepository|MongoDB sync skipped|MongoDB connection failed|dual-write' || true

docker compose logs --since=15m auth-query-service \
  | grep -Ei 'RabbitMQ Config|Listening on|Mongo|BrokerUnreachable|Failed|Error' || true
```

### Kiem tra env trong container (rat hay sai tren VPS)

```bash
docker exec soundmates-auth-service sh -lc 'printenv | grep -E "MONGODB_CONNECTION_STRING|MONGODB_DATABASE|ConnectionStrings__MongoDb|Mongo__Database|RABBITMQ_HOST|RABBITMQ_PORT"'
docker exec soundmates-auth-query-service sh -lc 'printenv | grep -E "MONGODB_CONNECTION_STRING|MONGODB_DATABASE|ConnectionStrings__MongoDb|Mongo__Database|RABBITMQ_HOST|RABBITMQ_PORT"'
```

Ky vong:
- `MONGODB_CONNECTION_STRING=mongodb://mongodb:27017`
- `MONGODB_DATABASE=auth_query`
- RabbitMQ host/port dung (`rabbitmq`, `5672`).

### Kiem tra queue consumer cua auth-query-service

```bash
docker exec soundmates-rabbitmq rabbitmqctl list_queues name messages consumers \
  | grep -E 'auth-service-query.users|auth-service-query.roles|auth-service-query.activity' || true
```

Ky vong: 3 queue tren co `consumers > 0`.

### Kiem tra du lieu trong MongoDB

```bash
docker exec soundmates-mongodb mongosh "mongodb://localhost:27017/auth_query" --eval "db.getCollectionNames()"
docker exec soundmates-mongodb mongosh "mongodb://localhost:27017/auth_query" --eval "db.user_favourites_read.countDocuments()"
docker exec soundmates-mongodb mongosh "mongodb://localhost:27017/auth_query" --eval "db.users_read.countDocuments()"
```

Neu ket qua la `[]`, `0`, `0` trong khi rabbitmq/auth-query-service van healthy, kha nang cao la chua co du lieu/event de dong bo (khong phai loi ket noi Mongo).

### Kiem tra nguon du lieu o PostgreSQL + Outbox

```bash
docker exec soundmates-postgres psql -U postgres -d auth_db -c "select count(*) as users_count from users;"
docker exec soundmates-postgres psql -U postgres -d auth_db -c "select count(*) as favourites_count from user_favourites;"

docker exec soundmates-postgres psql -U postgres -d auth_db -c "select count(*) as outbox_total, count(*) filter (where processed_on_utc is null) as outbox_pending from outbox_messages;"
docker exec soundmates-postgres psql -U postgres -d auth_db -c "select type, count(*) from outbox_messages group by type order by count(*) desc;"
docker exec soundmates-postgres psql -U postgres -d auth_db -c "select type, occurred_on_utc, processed_on_utc, error from outbox_messages order by occurred_on_utc desc limit 20;"
```

Doc ket qua:
- Neu `users_count`/`favourites_count` = 0: he thong chua co data nguon.
- Neu Postgres co data nhung outbox rat it hoac khong co event user/favourite: read-side se khong tu dong backfill lich su.
- Neu `outbox_pending` > 0 va `error` co gia tri: auth-service publish event dang loi.

### Fix nhanh theo dau hieu log

1. Neu `auth-service` bao `MongoDB connection failed` hoac `MongoDB sync skipped`:
   - Sua `.env` cho dung `MONGODB_CONNECTION_STRING` va `MONGODB_DATABASE`.
   - Recreate lai `mongodb` + `auth-service`.

2. Neu queue `auth-service-query.*` co `messages` tang va `consumers=0`:
   - `auth-query-service` khong consume duoc event.
   - Recreate `auth-query-service` va xem log startup RabbitMQ.

3. Neu Mongo co du lieu cho favourite nhung API read khong thay:
   - Kiem tra collection name/doc shape read-side va log handler trong `auth-query-service`.

4. Neu Mongo trong, queue trong, services healthy:
  - Day la trang thai "chua co event de projection".
  - Tao 1 thay doi nguon (VD tao user moi, update user, tao favourite) de phat event, roi kiem tra lai Mongo count.

Lenh recreate de nhan env moi:

```bash
docker compose --env-file .env up -d --build --force-recreate --no-deps mongodb rabbitmq auth-service auth-query-service
docker compose logs --since=5m -f auth-service auth-query-service
```

### Reset MongoDB (drop va cai lai)

Canh bao:
- Buoc nay xoa toan bo read-side data trong Mongo (`users_read`, `roles_read`, `user_favourites_read`, ...).
- Khong xoa du lieu nguon trong PostgreSQL (`auth_db`).
- Sau khi reset, Mongo co the van rong neu khong co event moi hoac khong chay backfill.

Option A - Drop rieng database `auth_query` (nhanh):

```bash
cd /opt/soundmates-be/soundmates-be-prj

# (Khuyen nghi) backup nhanh truoc khi drop
mkdir -p /tmp/mongo-backup
docker exec soundmates-mongodb sh -lc 'mongodump --uri="mongodb://localhost:27017/auth_query" --out=/tmp/mongo-dump'
docker cp soundmates-mongodb:/tmp/mongo-dump /tmp/mongo-backup/

# Drop database read-side
docker exec soundmates-mongodb mongosh "mongodb://localhost:27017/auth_query" --eval "db.dropDatabase()"

# Restart consumer de tao lai index + consume event moi
docker compose up -d --build --force-recreate --no-deps auth-query-service
docker compose logs --since=5m auth-query-service | tail -n 120
```

Option B - Drop han volume MongoDB (sach hoan toan):

```bash
cd /opt/soundmates-be/soundmates-be-prj

# 1) Dung service phu thuoc Mongo
docker compose stop auth-query-service auth-service

# 2) Dung va xoa container mongodb
docker compose stop mongodb
docker compose rm -sf mongodb

# 3) Tim ten volume Mongo va xoa
docker volume ls | grep mongodb-data
docker volume rm <ten-volume-mongodb-data-o-tren>

# 4) Tao lai Mongo + service consume/publish lien quan
docker compose up -d mongodb
docker compose up -d --build --force-recreate --no-deps auth-service auth-query-service

# 5) Verify Mongo moi
docker compose ps mongodb auth-service auth-query-service
docker exec soundmates-mongodb mongosh "mongodb://localhost:27017/auth_query" --eval "db.getCollectionNames()"
```

Sau khi reset Mongo:
- Neu `users_read` van rong, chay lai muc `Backfill one-time tu PostgreSQL sang read-side MongoDB` o ben duoi.
- Neu log auth-query-service van lap `Failed: auth.user.updated`, chay quy trinh xu ly NACK/requeue o muc `Case: login OK ... 404 User not found`.

### Probe nhanh pipeline (khong can goi API)

Muc tieu: chen 1 outbox event test vao Postgres. Neu auth-query-service consume duoc, Mongo se co 1 document trong `users_read`.

```bash
docker exec soundmates-postgres psql -U postgres -d auth_db -c "insert into outbox_messages (id, type, payload, occurred_on_utc, processed_on_utc, error) values (uuid_generate_v4(), 'auth.user.created', '{\"id\":\"11111111-1111-1111-1111-111111111111\",\"username\":\"mongo_probe_user\",\"email\":\"mongo_probe_user@example.com\",\"firstName\":\"Mongo\",\"lastName\":\"Probe\",\"roleName\":\"User\",\"isActive\":true,\"isVerified\":true}', now() at time zone 'utc', null, null);"

sleep 5

docker compose logs --since=2m auth-service | grep -Ei 'Processed [0-9]+ outbox messages|Failed to publish message|Outbox publisher loop failed' || true
docker compose logs --since=2m auth-query-service | grep -Ei 'User created in MongoDB|Failed: auth.user.created|No handler' || true

# Luu y: "Processed N outbox messages" la so ban ghi duoc quet, khong dong nghia publish thanh cong 100%.
# Kiem tra ket qua that su trong outbox:
docker exec soundmates-postgres psql -U postgres -d auth_db -c "select type, occurred_on_utc, processed_on_utc, error from outbox_messages order by occurred_on_utc desc limit 20;"

docker exec soundmates-mongodb mongosh "mongodb://localhost:27017/auth_query" --eval "db.users_read.findOne({_id: UUID('11111111-1111-1111-1111-111111111111')})"
docker exec soundmates-mongodb mongosh "mongodb://localhost:27017/auth_query" --eval "db.users_read.countDocuments()"
```

Neu probe thanh cong thi ket luan:
- Duong sync hoat dong.
- Ly do Mongo rong la do chua co event nghiep vu phat ra (hoac da reset DB ma khong co co che backfill lich su).

### Case: login OK nhung `/api/v1/users/me/profile/full` tra `404 User not found`

Dau hieu nay thuong la thieu projection cua user trong `users_read` (auth-query read-side).

```bash
cd /opt/soundmates-be/soundmates-be-prj

# 1) Lay user id auth-query dang doc tu token (tu log request that bai)
docker compose logs --since=15m auth-query-service \
  | grep -E 'Retrieving full profile for authenticated user|Full profile not found for user|Invalid or missing user token in GetMyFullProfile'

# 2) Gan USER_ID theo log tren (GUID)
export USER_ID=<guid-tu-log>

# 3) Xac minh projection hien co trong Mongo hay chua
docker exec soundmates-mongodb mongosh "mongodb://localhost:27017/auth_query" --eval "db.users_read.findOne({_id: UUID('${USER_ID}')},{_id:1,Username:1,Email:1,RoleName:1,UpdatedAt:1})"

# 4) Neu null -> day 1 su kien user update cho dung user do
docker exec soundmates-postgres psql -U postgres -d auth_db -c "insert into outbox_messages (id, type, payload, occurred_on_utc, processed_on_utc, error) select uuid_generate_v4(), 'auth.user.updated', json_build_object('id', u.id::text, 'username', u.username, 'email', u.email, 'firstName', coalesce(u.first_name, ''), 'lastName', coalesce(u.last_name, ''), 'roleId', case when u.role_id is null then null else u.role_id::text end, 'roleName', coalesce(ur.name, 'User'), 'isActive', coalesce(u.is_active, true), 'isVerified', (u.email_verified_at is not null), 'emailVerifiedAt', u.email_verified_at)::text, now() at time zone 'utc', null, null from users u left join user_roles ur on ur.id = u.role_id where u.id = '${USER_ID}'::uuid;"

sleep 5

# 5) Verify outbox + projection sau khi publisher xu ly
docker exec soundmates-postgres psql -U postgres -d auth_db -c "select type, processed_on_utc, error from outbox_messages where type like 'auth.user.%' order by occurred_on_utc desc limit 10;"
docker compose logs --since=2m auth-query-service | grep -Ei 'Processed: auth.user.updated|User updated in MongoDB|Failed: auth.user.updated|No handler' || true
docker exec soundmates-mongodb mongosh "mongodb://localhost:27017/auth_query" --eval "db.users_read.findOne({_id: UUID('${USER_ID}')},{_id:1,Username:1,Email:1,RoleName:1,UpdatedAt:1})"
```

Neu thay log bi lap vo han `Failed: auth.user.updated` (NACK/requeue), dung quy trinh sau:

```bash
# A) Lay du stack trace (khong chi grep 1 dong)
docker compose logs --since=5m auth-query-service \
  | sed -n '/Failed: auth.user.updated/,+40p'

# B) Loc nhanh cac dau hieu loi thuong gap
docker compose logs --since=5m auth-query-service \
  | grep -Ei 'MongoWriteException|E11000|duplicate key|JsonException|Required string property not found|Required Guid property not found' || true

# C) Kiem tra duplicate username/email trong users_read (nguyen nhan pho bien)
USER_INFO=$(docker exec soundmates-postgres psql -U postgres -d auth_db -At -F '|' -c "select coalesce(username,''), coalesce(email,'') from users where id='${USER_ID}'::uuid;")
USERNAME=$(echo "$USER_INFO" | cut -d'|' -f1)
EMAIL=$(echo "$USER_INFO" | cut -d'|' -f2)

echo "USERNAME=$USERNAME"
echo "EMAIL=$EMAIL"

docker exec soundmates-mongodb mongosh "mongodb://localhost:27017/auth_query" --eval "db.users_read.find({\$or:[{Username:'${USERNAME}'},{Email:'${EMAIL}'}]},{_id:1,Username:1,Email:1,AccountStatus:1,UpdatedAt:1}).toArray()"

# D) Neu co doc trung username/email nhung _id khac USER_ID thi xoa doc stale
docker exec soundmates-mongodb mongosh "mongodb://localhost:27017/auth_query" --eval "db.users_read.deleteMany({_id:{\$ne: UUID('${USER_ID}')},\$or:[{Username:'${USERNAME}'},{Email:'${EMAIL}'}]})"

# E) Day lai 1 event auth.user.updated de re-project user dang loi
docker exec soundmates-postgres psql -U postgres -d auth_db -c "insert into outbox_messages (id, type, payload, occurred_on_utc, processed_on_utc, error) select uuid_generate_v4(), 'auth.user.updated', json_build_object('id', u.id::text, 'username', coalesce(u.username, 'user_' || replace(u.id::text, '-', '')), 'email', coalesce(u.email, 'user_' || replace(u.id::text, '-', '') || '@local.invalid'), 'firstName', coalesce(u.first_name, ''), 'lastName', coalesce(u.last_name, ''), 'roleId', case when u.role_id is null then null else u.role_id::text end, 'roleName', coalesce(ur.name, 'User'), 'isActive', coalesce(u.is_active, true), 'isVerified', (u.email_verified_at is not null), 'emailVerifiedAt', u.email_verified_at)::text, now() at time zone 'utc', null, null from users u left join user_roles ur on ur.id = u.role_id where u.id = '${USER_ID}'::uuid;"

sleep 5
docker compose logs --since=2m auth-query-service | grep -Ei 'User updated in MongoDB|Processed: auth.user.updated|Failed: auth.user.updated' || true
docker exec soundmates-mongodb mongosh "mongodb://localhost:27017/auth_query" --eval "db.users_read.findOne({_id: UUID('${USER_ID}')},{_id:1,Username:1,Email:1,RoleName:1,UpdatedAt:1})"
```

Neu lenh insert outbox tra ve `INSERT 0 0`, co nghia `USER_ID` khong ton tai trong `auth_db.users` (token cu/sai user).
Neu outbox da `processed_on_utc` nhung Mongo van null, quay lai buoc kiem tra queue consumer (`auth-service-query.users`) va log auth-query-service.

### Backfill one-time tu PostgreSQL sang read-side MongoDB

Dung khi PostgreSQL da co user/role nhung Mongo rong sau khi deploy moi.

```bash
# 1) Backfill role events
docker exec soundmates-postgres psql -U postgres -d auth_db -c "insert into outbox_messages (id, type, payload, occurred_on_utc, processed_on_utc, error) select uuid_generate_v4(), 'auth.role.created', json_build_object('id', ur.id::text, 'name', ur.name, 'description', null)::text, now() at time zone 'utc', null, null from user_roles ur;"

# 2) Backfill user events
docker exec soundmates-postgres psql -U postgres -d auth_db -c "insert into outbox_messages (id, type, payload, occurred_on_utc, processed_on_utc, error) select uuid_generate_v4(), 'auth.user.created', json_build_object('id', u.id::text, 'username', u.username, 'email', u.email, 'firstName', coalesce(u.first_name, ''), 'lastName', coalesce(u.last_name, ''), 'roleId', case when u.role_id is null then null else u.role_id::text end, 'roleName', coalesce(ur.name, 'User'), 'isActive', coalesce(u.is_active, true), 'isVerified', (u.email_verified_at is not null), 'emailVerifiedAt', u.email_verified_at)::text, now() at time zone 'utc', null, null from users u left join user_roles ur on ur.id = u.role_id;"

# 3) Cho outbox publisher xu ly va verify Mongo
sleep 10
docker compose logs --since=2m auth-service | grep -Ei 'Processed [0-9]+ outbox messages|Failed to publish message|Outbox publisher loop failed' || true
docker exec soundmates-mongodb mongosh "mongodb://localhost:27017/auth_query" --eval "db.roles_read.countDocuments()"
docker exec soundmates-mongodb mongosh "mongodb://localhost:27017/auth_query" --eval "db.users_read.countDocuments()"
```

## 19) Loi `database "account_content_db" does not exist` (account-content-service)

Loi nay xay ra khi DB `account_content_db` chua duoc tao trong Postgres, trong khi service da tro den DB nay.

### Fix ngay tren Ubuntu (khong can reset volume)

```bash
cd /opt/soundmates-be/soundmates-be-prj

# 1) Xac minh service dang dung DB nao
docker exec soundmates-account-content-service sh -lc 'printenv | grep -E "ConnectionStrings__DefaultConnection|ACCOUNT_CONTENT_DB_NAME|DB_HOST|DB_PORT|DB_USER"'

# 2) Kiem tra DB da ton tai chua
docker exec soundmates-postgres psql -U postgres -d postgres -c "select datname from pg_database where datname='account_content_db';"

# 3) Neu chua co thi tao DB
docker exec soundmates-postgres psql -U postgres -d postgres -c "CREATE DATABASE account_content_db;"

# 4) Recreate account-content-service de app chay migration + seed lai
docker compose --env-file .env up -d --build --force-recreate --no-deps account-content-service

# 5) Theo doi log startup migration
docker compose logs --since=5m -f account-content-service | grep -Ei 'Starting database migration|Database migration completed|Seed|error|failed'
```

### Kiem tra ket qua

```bash
docker exec soundmates-postgres psql -U postgres -d account_content_db -c "\dt"
curl -s -o /dev/null -w "%{http_code}\n" "http://127.0.0.1:8080/api/v1/posts/published?page=1&pageSize=12"
```

Neu tra ve `200`/`204` hoac khong con `3D000`, da khac phuc.

### De khong tai dien tren may moi

- Repo da co file `docker/postgres/init-multiple-dbs.sql`.
- Dam bao `docker-compose.yml` mount file nay vao `/docker-entrypoint-initdb.d/`.
- Luu y: script init chi chay khi `postgres-data` la volume moi (lan dau khoi tao).

## 20) AzuraCast tao station moi bao `This installation has no available ports for new radio stations`

Loi nay xay ra khi pool port auto-assign cua AzuraCast da het (hoac bi cau hinh qua hep), nen he thong khong tim duoc block port trong range de cap cho station moi.

Theo code AzuraCast:
- Port default: 8000 -> 8499.
- Moi station can 1 block 10 port (base port + cac port lien quan).

### Kiem tra nhanh pool port hien tai

```bash
cd /var/azuracast

grep -n '^AZURACAST_STATION_PORTS=' .env

PORT_COUNT=$(grep '^AZURACAST_STATION_PORTS=' .env | cut -d '=' -f2- | awk -F',' '{print NF}')
echo "AZURACAST_STATION_PORTS count=$PORT_COUNT"

API_KEY=$(grep '^AZURACAST_API_KEY=' /opt/soundmates-be/soundmates-be-prj/.env | cut -d '=' -f2-)
curl -s -H "X-API-Key: ${API_KEY}" http://127.0.0.1:5000/api/admin/stations \
  | jq -r '.[] | [.id, .name, (.frontend_config.port // "-"), (.backend_config.dj_port // "-"), (.backend_config.telnet_port // "-")] | @tsv'
```

Neu `PORT_COUNT` qua nho (hoac station count lon gan het suc chua), can mo rong pool.

### Mo rong pool station ports (vi du den 8999)

```bash
cd /var/azuracast
cp .env ".env.bak.$(date +%F-%H%M%S)"

NEW_PORTS=$(for p in $(seq 8000 10 8990); do printf "%s,%s,%s," "$p" "$((p+5))" "$((p+6))"; done | sed 's/,$//')
sed -i "s#^AZURACAST_STATION_PORTS=.*#AZURACAST_STATION_PORTS=${NEW_PORTS}#" .env

grep -n '^AZURACAST_STATION_PORTS=' .env
```

Ap cau hinh moi:

```bash
cd /var/azuracast
./docker.sh update
docker compose ps
```

### Verify sau khi cap nhat

```bash
API_KEY=$(grep '^AZURACAST_API_KEY=' /opt/soundmates-be/soundmates-be-prj/.env | cut -d '=' -f2-)
curl -s -H "X-API-Key: ${API_KEY}" http://127.0.0.1:5000/api/admin/station/1 \
  | jq -r '.name, .frontend_config.port, .backend_config.dj_port, .backend_config.telnet_port'
```

Sau do tao lai station moi trong UI/API.

### Luu y firewall

Neu ban cho phep nghe truc tiep qua dedicated radio ports, mo them range tren firewall:

```bash
ufw allow 8000:8999/tcp
ufw reload
ufw status | grep -E '8000:8999|8000/tcp'
```

Neu ban chi route nghe qua web port (5000/80/443), co the khong can expose toan bo range ra Internet.

## 19) "Unknown User" trong bai viet / Forum (UserProfileReadModel bi thieu)

### Nguyen nhan

`account-content-service` lay ten hien thi tu bang `user_profile_read_models` trong DB `account_content_db`.
Bang nay duoc populate qua su kien RabbitMQ `auth.user.created` (publish boi `auth-service`).
Neu event nay bi miss (service chua ready, RabbitMQ chua chay, hoac deploy moi), ban ghi se thieu
va moi bai viet tra ve `"Unknown User"`.

### Buoc 1: Kiem tra nhanh

```bash
cd /opt/soundmates-be/soundmates-be-prj

# Dem so user trong UserProfileReadModel (account_content_db)
docker exec soundmates-postgres psql -U postgres -d account_content_db -c \
  "SELECT count(*) AS profile_count FROM user_profile_read_models;"

# Dem so user thuc te trong auth_db
docker exec soundmates-postgres psql -U postgres -d auth_db -c \
  "SELECT count(*) AS user_count FROM users;"
```

Neu `profile_count` < `user_count`: can backfill.

### Buoc 2: Kiem tra log UserEventConsumer

```bash
docker compose logs --since=30m account-content-service \
  | grep -Ei 'UserEventConsumer|UserProfileReadModel upserted|Failed to process message|BrokerUnreachable|Listening on queue' | tail -n 40
```

Ky vong: `UserEventConsumer started. Listening on queue account-content.user-events`

### Buoc 3: Backfill toan bo users (idempotent - consumer dung Upsert)

```bash
cd /opt/soundmates-be/soundmates-be-prj

docker exec soundmates-postgres psql -U postgres -d auth_db -c "
INSERT INTO outbox_messages (id, type, payload, occurred_on_utc, processed_on_utc, error)
SELECT
  uuid_generate_v4(),
  'auth.user.created',
  json_build_object(
    'id',          u.id::text,
    'username',    coalesce(u.username, ''),
    'email',       coalesce(u.email, ''),
    'firstName',   coalesce(u.first_name, ''),
    'lastName',    coalesce(u.last_name, ''),
    'roleId',      case when u.role_id is null then null else u.role_id::text end,
    'roleName',    coalesce(ur.name, 'User'),
    'isActive',    coalesce(u.is_active, true),
    'isVerified',  (u.email_verified_at is not null),
    'createdAt',   u.created_at
  )::text,
  now() at time zone 'utc',
  null, null
FROM users u
LEFT JOIN user_roles ur ON ur.id = u.role_id;
"

echo "Da insert outbox events. Cho 10 giay de OutboxPublisher xu ly..."
sleep 10

# Kiem tra outbox da duoc xu ly chua
docker exec soundmates-postgres psql -U postgres -d auth_db -c \
  "SELECT
     count(*) filter(where processed_on_utc is null) AS pending,
     count(*) filter(where processed_on_utc is not null) AS processed,
     count(*) filter(where error is not null) AS errors
   FROM outbox_messages WHERE type = 'auth.user.created'
   AND occurred_on_utc > now() - interval '5 minutes';"

# Sau khi OutboxPublisher xu ly xong, kiem tra account_content_db
docker exec soundmates-postgres psql -U postgres -d account_content_db -c \
  "SELECT count(*) AS profile_count FROM user_profile_read_models;"
```

### Buoc 4: Kiem tra log account-content-service tiep nhan event

```bash
docker compose logs --since=3m account-content-service \
  | grep -Ei 'UserProfileReadModel upserted|Failed to process message' | tail -n 40
```

Ky vong: `UserProfileReadModel upserted for user {UserId} (created)` x N users.

### Buoc 5 (neu con loi): Neu outbox.error khong null — RabbitMQ publish dang loi

```bash
docker exec soundmates-postgres psql -U postgres -d auth_db -c \
  "SELECT id, type, occurred_on_utc, processed_on_utc, error
   FROM outbox_messages WHERE type = 'auth.user.created'
   ORDER BY occurred_on_utc DESC LIMIT 20;"

# Neu co error, restart auth-service de thu lai publish
docker compose restart auth-service
sleep 5
docker compose logs --since=2m auth-service \
  | grep -Ei 'Processed [0-9]+ outbox|Failed to publish' | tail -n 20
```

### Buoc 6 (optional): Kiem tra profile cu the cho 1 user bi loi

```bash
export USER_ID=<GUID-CUA-USER>

docker exec soundmates-postgres psql -U postgres -d account_content_db -c \
  "SELECT id, full_name, email, is_pending, updated_at
   FROM user_profile_read_models WHERE id = '${USER_ID}'::uuid;"
```

## 20) Upload file bao loi 413 Content Too Large (Nginx chan truoc khi den BE)

### Nguyen nhan

Web deploy qua Nginx → request upload lon bi chan boi Nginx truoc khi den API gateway.
Local dev goi thang toi BE (khong qua Nginx) nen khong bi loi.

### Fix nhanh

```bash
# Kiem tra config hien tai
grep -n 'client_max_body_size' /etc/nginx/sites-available/soundmates-web || echo "Chua co, dang dung mac dinh 1MB"
grep -n 'client_max_body_size' /etc/nginx/nginx.conf

# Them hoac sua trong server block cua soundmates-web
# Them dong nay vao ben trong block server{} (truoc hoac sau location):
#   client_max_body_size 100M;

# Chinh sua file
nano /etc/nginx/sites-available/soundmates-web
```

Noi dung can co trong file config (vi du):

```nginx
server {
    listen 80;
    server_name soundmates.xyz www.soundmates.xyz;

    client_max_body_size 100M;   # ← dam bao co dong nay

    location /api/ {
        proxy_pass http://127.0.0.1:8080;
        proxy_read_timeout 300s;
        proxy_send_timeout 300s;
        # ...
    }

    location / {
        root /opt/soundmates-web/soundmates-web-prj/dist;
        try_files $uri $uri/ /index.html;
    }
}
```

```bash
# Test config truoc khi reload
nginx -t

# Neu OK, reload nginx
systemctl reload nginx

# Verify
grep -n 'client_max_body_size' /etc/nginx/sites-available/soundmates-web
```

### Kiem tra nhanh khi gap 413

```bash
# Kiem tra status code thuc te
curl -s -o /dev/null -w "%{http_code}" -X POST \
  "https://api.soundmates.xyz/api/v1/media/upload" \
  -H "Content-Type: multipart/form-data" \
  -F "file=@/tmp/test.mp3" | head -c 5

# Xem log nginx loi
tail -n 50 /var/log/nginx/error.log | grep -i '413\|too large\|body'
```

## 26) TURN Server (Coturn) - WebRTC Host Mic Relay

TURN server giup Host Mic hoat dong qua moi mang NAT/Firewall khi WebRTC P2P bi chan.

### Lan dau cai dat Coturn tren VPS

```bash
cd /opt/soundmates-be/soundmates-be-prj

# 1) Lay IP noi bo cua VPS (TURN_RELAY_IP)
ip a | grep -oP 'inet \K[\d.]+' | grep -v '127.0.0.1' | head -n 1

# 2) Lay IP public cua VPS (TURN_EXTERNAL_IP - phai la 161.97.85.232)
curl -s ifconfig.me

# 3) Cap nhat TURN_RELAY_IP trong .env neu can
#    (TURN_EXTERNAL_IP da la 161.97.85.232 roi)
nano .env
# Doi TURN_RELAY_IP thanh IP noi bo correct (vi du 10.0.0.1)

# 4) Mo cong tuong lua cho TURN (quan trong!)
ufw allow 3478/tcp
ufw allow 3478/udp
ufw allow 5349/tcp
ufw allow 5349/udp
ufw allow 49152:65535/udp
ufw reload
ufw status

# 5) Khoi dong coturn container
docker compose up -d coturn

# 6) Kiem tra coturn dang chay
docker ps | grep coturn
docker logs soundmates-coturn --tail 30
```

### Kiem tra TURN hoat dong

```bash
# Test ket noi TURN tu may ngoai VPS
# Dung trang web: https://webrtc.github.io/samples/src/content/peerconnection/trickle-ice/
# Dien:
#   STUN or TURN URI: turn:161.97.85.232:3478
#   Username: soundmates
#   Password: SoundmatesTurn@2024!
# Bam Add Server -> Gather candidates
# Ky vong thay candidate loai "relay" -> TURN hoat dong

# Kiem tra log coturn co phat hien ket noi
docker logs -f soundmates-coturn | grep -E 'session|allocat|relay'
```

### Neu coturn restart hoac sau khi deploy lai

```bash
cd /opt/soundmates-be/soundmates-be-prj

# Chi khoi dong lai coturn, khong rebuild
docker compose up -d coturn
docker logs soundmates-coturn --tail 20
```

### Deploy frontend sau khi cap nhat TURN config

```bash
cd /opt/soundmates-web/soundmates-web-prj

# Kiem tra .env tren server co cac bien TURN
grep -E '^VITE_TURN' .env

# Neu chua co, them vao
cat >> .env << 'EOF'
VITE_TURN_HOST=161.97.85.232
VITE_TURN_USERNAME=soundmates
VITE_TURN_PASSWORD=SoundmatesTurn@2024!
EOF

# Build lai frontend
npm ci
npm run build
systemctl reload nginx
```

