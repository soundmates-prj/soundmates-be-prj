using System.ComponentModel.DataAnnotations;

namespace AccountContentService.Domain.Entities;

/// <summary>
/// Luu tru thong tin gioc noi AI cua nguoi dung
/// Voice clone: nguoi dung upload audio → he thong clone gioc → luu model
/// </summary>
public class UserVoiceModel
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Nguoi dung so huu gioc nay
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Ten hien thi cua gioc (do nguoi dung dat)
    /// </summary>
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Ma gioc ben AI service (voice_id ben vieneu-tts)
    /// </summary>
    [MaxLength(100)]
    public string VoiceCode { get; set; } = string.Empty;

    /// <summary>
    /// Nha cung cap AI (ViNeuTTS, etc.)
    /// </summary>
    [MaxLength(50)]
    public string Provider { get; set; } = "ViNeuTTS";

    /// <summary>
    /// Thoi gian audio nguon (giay) - de tinh phi clone
    /// </summary>
    public int SourceAudioDurationSeconds { get; set; }

    /// <summary>
    /// Duong dan den file audio nguon (da clone)
    /// </summary>
    [MaxLength(500)]
    public string? SourceAudioUrl { get; set; }

    /// <summary>
    /// Trang thai: pending, ready, failed, deleted
    /// </summary>
    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Thoi gian tao
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Thoi gian cap nhat cuoi
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
