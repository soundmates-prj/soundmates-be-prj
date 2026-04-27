using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using LiveSessionService.Application.Abstractions;
using LiveSessionService.Infrastructure.Services.AzuraCast;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace LiveSessionService.Infrastructure.Services;

public sealed class AzuraCastPodcastService : IAzuraCastPodcastService
{
    private readonly HttpClient _httpClient;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly ILogger<AzuraCastPodcastService> _logger;

    public AzuraCastPodcastService(
        HttpClient httpClient,
        IAzuraCastClient azuraCastClient,
        ILogger<AzuraCastPodcastService> logger)
    {
        _httpClient = httpClient;
        _azuraCastClient = azuraCastClient;
        _logger = logger;
    }

    public async Task<AzuraCastPodcastUploadResult> UploadAndQueuePodcastAsync(
        int stationId,
        string audioUrl,
        string title,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting podcast upload workflow for station {StationId}. URL: {Url}",
            stationId, audioUrl);

        // Step 1: Download audio from ai-service URL
        byte[] audioBytes;
        string extension;

        try
        {
            using var downloadResponse = await _httpClient.GetAsync(audioUrl, cancellationToken);
            downloadResponse.EnsureSuccessStatusCode();

            audioBytes = await downloadResponse.Content.ReadAsByteArrayAsync(cancellationToken);
            extension = Path.GetExtension(audioUrl)?.ToLowerInvariant() ?? ".wav";

            _logger.LogInformation(
                "Downloaded audio ({Size} bytes, ext={Ext}) from {Url}",
                audioBytes.Length, extension, audioUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download audio from {Url}", audioUrl);
            return new AzuraCastPodcastUploadResult(
                IsSuccess: false,
                MediaId: null,
                ErrorMessage: $"Failed to download audio: {ex.Message}");
        }

        // Step 2: Convert to MP3 if needed
        byte[] mp3Bytes;
        if (extension == ".mp3")
        {
            mp3Bytes = audioBytes;
            _logger.LogInformation("Audio is already MP3, skipping conversion");
        }
        else
        {
            _logger.LogInformation("Converting {Ext} to MP3", extension);
            var convertResult = await ConvertToMp3Async(audioBytes, cancellationToken);
            if (!convertResult.IsSuccess)
            {
                return new AzuraCastPodcastUploadResult(
                    IsSuccess: false,
                    MediaId: null,
                    ErrorMessage: convertResult.ErrorMessage);
            }
            mp3Bytes = convertResult.Bytes!;
        }

        // Step 3: Upload to AzuraCast
        var safeTitle = SanitizeFileName(title);
        var fileName = $"{safeTitle}_{DateTime.UtcNow:yyyyMMddHHmmss}.mp3";

        _logger.LogInformation(
            "Uploading MP3 ({Size} bytes) to AzuraCast station {StationId} as '{FileName}'",
            mp3Bytes.Length, stationId, fileName);

        await using var stream = new MemoryStream(mp3Bytes);
        var mediaData = await _azuraCastClient.UploadMediaAsync(
            stationId,
            stream,
            fileName,
            "audio/mpeg",
            title,
            "AI Podcast",
            null,
            cancellationToken);

        if (mediaData == null || string.IsNullOrWhiteSpace(mediaData.UniqueId))
        {
            _logger.LogError("AzuraCast did not return a media ID after upload");
            return new AzuraCastPodcastUploadResult(
                IsSuccess: false,
                MediaId: null,
                ErrorMessage: "AzuraCast upload succeeded but returned no media ID");
        }

        _logger.LogInformation(
            "Successfully uploaded podcast to AzuraCast. MediaId: {MediaId}",
            mediaData.UniqueId);

        return new AzuraCastPodcastUploadResult(
            IsSuccess: true,
            MediaId: mediaData.UniqueId,
            ErrorMessage: null);
    }

    private async Task<(bool IsSuccess, byte[]? Bytes, string? ErrorMessage)> ConvertToMp3Async(
        byte[] inputBytes,
        CancellationToken cancellationToken)
    {
        // Write input to temp file
        var tempWavPath = Path.Combine(Path.GetTempPath(), $"podcast_in_{Guid.NewGuid():N}.wav");
        var tempMp3Path = Path.Combine(Path.GetTempPath(), $"podcast_out_{Guid.NewGuid():N}.mp3");

        try
        {
            await File.WriteAllBytesAsync(tempWavPath, inputBytes, cancellationToken);

            // Try FFmpeg first (most reliable for .NET in Docker/Linux)
            var ffmpegResult = await TryFFmpegAsync(tempWavPath, tempMp3Path, cancellationToken);
            if (ffmpegResult.Success)
            {
                var mp3Bytes = await File.ReadAllBytesAsync(tempMp3Path, cancellationToken);
                _logger.LogInformation("FFmpeg conversion successful: {Size} bytes", mp3Bytes.Length);
                return (true, mp3Bytes, null);
            }

            // Fallback: NAudio WAV passthrough (AzuraCast may handle non-MP3)
            // Some AzuraCast versions accept WAV/OGG directly
            var naudioResult = await TryNAudioPassthroughAsync(tempWavPath, cancellationToken);
            if (naudioResult.Success)
            {
                _logger.LogWarning(
                    "Audio conversion failed (FFmpeg: {FFmpegError}). AzuraCast will try to handle the file directly.",
                    ffmpegResult.ErrorMessage);
                // Return the original bytes — AzuraCast may handle it
                return (true, inputBytes, ffmpegResult.ErrorMessage);
            }

            return (false, null, $"Conversion failed. FFmpeg: {ffmpegResult.ErrorMessage}");
        }
        finally
        {
            try { if (File.Exists(tempWavPath)) File.Delete(tempWavPath); } catch { }
            try { if (File.Exists(tempMp3Path)) File.Delete(tempMp3Path); } catch { }
        }
    }

    private async Task<(bool Success, string? ErrorMessage)> TryFFmpegAsync(
        string inputPath,
        string outputPath,
        CancellationToken cancellationToken)
    {
        try
        {
            var args = $"-y -i \"{inputPath}\" -codec:a libmp3lame -q:a 2 \"{outputPath}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                return (false, "Could not start FFmpeg process");

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0 && File.Exists(outputPath))
                return (true, null);

            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            return (false, error.Trim());
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private Task<(bool Success, string? ErrorMessage)> TryNAudioPassthroughAsync(
        string wavPath,
        CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new WaveFileReader(wavPath);
            // Validate it's a readable WAV
            var valid = reader.WaveFormat.Encoding is
                WaveFormatEncoding.Pcm or
                WaveFormatEncoding.IeeeFloat;
            return Task.FromResult(valid ? (true, (string?)null) : (false, "Not a valid WAV file"));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, ex.Message));
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name);
        foreach (var c in invalid)
            sb.Replace(c, '_');
        var result = sb.ToString().Trim();
        return string.IsNullOrEmpty(result) ? "podcast" : result;
    }
}