using Google.Apis.Auth.OAuth2;
using Google.Cloud.Speech.V1;
using Grpc.Auth;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LiveSessionService.Infrastructure.Services
{
    public class AudioModerationService : IAudioModerationService
    {
        private readonly ILogger<AudioModerationService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _credentialsJson;

        private static readonly string[] SevereWords = new[]
        {
            "giết", "ám sát", "đồ sát", "bắn chết", "đâm chém", "cắt cổ", "khủng bố", "chặt chém", "buôn ma túy",
            "tự sát", "tự tử", "chết đi", "nhảy lầu", "thắt cổ", "uống thuốc chuột",
            "hiếp dâm", "ấu dâm", "loạn luân", "cưỡng hiếp", 
            "nứng", "dâm", "quay tay", "làm tình", "thẩm du",
            "đĩ", "phò", "cave", "gái gọi", "đi khách",
            "địt", "đụ", "lồn", "cặc", "buồi", "dái", "đm", "đcm", "vcl", "vkl", "vl", "đmày", "đệt", "mẹ kiếp"
        };

        private static readonly string[] BadWords = new[]
        {
            "ngu", "óc chó", "khốn nạn", "chó", "điên", "súc vật", "rác rưởi", "cặn bã", "đồ lợn", "mất dạy"
        };

        // Precompiled regex for optimization
        private static readonly Regex SevereRegex = new Regex($@"(?<!\S)({string.Join("|", SevereWords.Select(Regex.Escape))})(?!\S)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex BadRegex = new Regex($@"(?<!\S)({string.Join("|", BadWords.Select(Regex.Escape))})(?!\S)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public AudioModerationService(ILogger<AudioModerationService> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            
            // Read from GOOGLE_CREDENTIALS_BASE64 environment variable or config
            var credentialsBase64 = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_BASE64") 
                ?? configuration["GoogleCloud:CredentialsBase64"];

            if (!string.IsNullOrWhiteSpace(credentialsBase64))
            {
                try
                {
                    var bytes = Convert.FromBase64String(credentialsBase64);
                    _credentialsJson = System.Text.Encoding.UTF8.GetString(bytes);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to decode GOOGLE_CREDENTIALS_BASE64");
                    _credentialsJson = string.Empty;
                }
            }
            else
            {
                // Fallback to plain JSON string if set
                _credentialsJson = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_JSON") 
                    ?? configuration["GoogleCloud:CredentialsJson"] 
                    ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(_credentialsJson))
            {
                _logger.LogWarning("GOOGLE_CREDENTIALS_JSON is not configured. Google Speech-to-Text may fail.");
            }
        }

        public async Task<ModerationResult> CheckToxicityAsync(string audioUrl, CancellationToken cancellationToken = default)
        {
            var result = new ModerationResult();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var audioBytes = await client.GetByteArrayAsync(audioUrl, cancellationToken);
                
                var speechClient = await new SpeechClientBuilder
                {
                    GoogleCredential = GoogleCredential.FromJson(_credentialsJson)
                }.BuildAsync(cancellationToken);

                var response = await speechClient.RecognizeAsync(new RecognizeRequest
                {
                    Config = new RecognitionConfig
                    {
                        LanguageCode = "vi-VN"
                    },
                    Audio = RecognitionAudio.FromBytes(audioBytes)
                });
                
                var transcripts = response.Results.Select(r => r.Alternatives.FirstOrDefault()?.Transcript).Where(t => t != null);
                var final_text = string.Join("\n", transcripts).Trim();
                if (string.IsNullOrEmpty(final_text))
                {
                    final_text = "[No speech detected]";
                }
                
                result.Transcript = final_text;
                return AnalyzeToxicity(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "STT Error");
                return new ModerationResult
                {
                    Action = "SAFE",
                    Transcript = "[Lỗi STT: " + ex.Message + "]",
                };
            }
        }
        
        private ModerationResult AnalyzeToxicity(ModerationResult result)
        {
            var text = result.Transcript.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(text) || text == "[no speech detected]")
            {
                result.Action = "SAFE";
                return result;
            }

            var severeMatches = SevereRegex.Matches(text);
            int severeCount = severeMatches.Count;
            var severeFound = severeMatches.Select(m => m.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var badMatches = BadRegex.Matches(text);
            int badCount = badMatches.Count;
            var badFound = badMatches.Select(m => m.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            result.ToxicityScore = Math.Min(1.0, badCount * 0.3 + severeCount * 0.5);
            result.InsultScore = Math.Min(1.0, badCount * 0.4);
            result.ProfanityScore = Math.Min(1.0, badCount * 0.5);

            if (severeCount > 0)
            {
                result.Action = "REJECT";
            }
            else if (badCount > 0)
            {
                result.Action = "PENDING_REVIEW";
            }
            else
            {
                result.Action = "SAFE";
            }

            var triggered = new List<string>();
            triggered.AddRange(severeFound);
            triggered.AddRange(badFound);
            result.TriggeredWords = triggered.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

            return result;
        }
    }
}
