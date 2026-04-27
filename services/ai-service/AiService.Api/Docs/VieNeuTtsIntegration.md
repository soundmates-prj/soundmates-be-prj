# VieNeu-TTS integration (sync API)

## 1) Provider routes expected by `ai-service`

`ai-service` gọi VieNeu-TTS theo **sync HTTP route** cấu hình qua biến môi trường:

- `POST {TTS_BASE_URL}{TTS_SYNTHESIZE_PATH}`

Mặc định hiện tại:

- `TTS_BASE_URL=http://localhost:8086`
- `TTS_SYNTHESIZE_PATH=/v1/tts/synthesize`

### Request payload (JSON)

```json
{
  "text": "...",
  "voice": "...",
  "speed": 1.0,
  "pitch": 0.0,
  "model": "pnnbao-ump/VieNeu-TTS",
  "format": "mp3",
  "prompt": "..."
}
```

### Response supported by `ai-service`

`ai-service` hỗ trợ 2 kiểu response:

1. **Binary audio** (`Content-Type: audio/*`)  
   - Ví dụ: `audio/mpeg`, `audio/wav`
2. **JSON chứa base64 audio**  
   - Các key được parse: `audioBase64`, `audio_base64`, `audio`, hoặc lồng trong `data` / `result`.

## 2) `ai-service` routes liên quan flow prompt -> mp3

- `POST /api/scripts/podcast:generate`  
  Tạo script từ LLM (prompting).
- `POST /api/scripts/{scriptId}/audio:generate`  
  Dùng script + voice để gọi VieNeu-TTS sync API và lưu file audio.
- `GET /api/audios/{audioId}/file`  
  Stream file audio đã lưu (mp3/wav).

## 3) Required env/config for production wiring

- `TTS_PROVIDER=vienetts`
- `TTS_BASE_URL=...`
- `TTS_API_KEY=...` (nếu provider yêu cầu)
- `TTS_MODEL=...`
- `TTS_SYNTHESIZE_PATH=...`
- `TTS_AUDIO_FORMAT=mp3`
- `TTS_PROMPT_TEMPLATE=...` (optional; placeholders: `{text}`, `{voice}`, `{speed}`, `{pitch}`)
- `TTS_TIMEOUT_SECONDS=120`

## 4) Note

VieNeu-TTS repo public có chế độ remote qua `/v1/chat/completions`. `ai-service` đang chuẩn hóa theo route sync audio (`/v1/tts/synthesize`) để nhận trực tiếp audio và lưu file mp3.
Nếu backend VieNeu của bạn dùng route khác, chỉ cần đổi `TTS_SYNTHESIZE_PATH`.
