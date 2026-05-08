using System.Threading;
using System.Threading.Tasks;

namespace LiveSessionService.Domain.Interfaces
{
    public interface IAudioModerationService
    {
        Task<ModerationResult> CheckToxicityAsync(string audioUrl, CancellationToken cancellationToken = default);
    }

    public class ModerationResult
    {
        public double ToxicityScore { get; set; }
        public double InsultScore { get; set; }
        public double ProfanityScore { get; set; }
        public string Action { get; set; } = string.Empty;
        public string[] TriggeredWords { get; set; } = System.Array.Empty<string>();
        public string Transcript { get; set; } = string.Empty;
    }
}
