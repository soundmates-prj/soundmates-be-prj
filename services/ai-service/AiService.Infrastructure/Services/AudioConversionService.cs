using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace AiService.Infrastructure.Services;

public interface IAudioConversionService
{
    Task<(bool IsSuccess, byte[]? Mp3Bytes, string? ErrorMessage)> ConvertToMp3Async(
        byte[] inputBytes,
        string inputExtension,
        CancellationToken cancellationToken = default);
}

public sealed class AudioConversionService : IAudioConversionService
{
    private readonly ILogger<AudioConversionService> _logger;

    public AudioConversionService(ILogger<AudioConversionService> logger)
    {
        _logger = logger;
    }

    public async Task<(bool IsSuccess, byte[]? Mp3Bytes, string? ErrorMessage)> ConvertToMp3Async(
        byte[] inputBytes,
        string inputExtension,
        CancellationToken cancellationToken = default)
    {
        // MP3 passthrough
        if (string.Equals(inputExtension, ".mp3", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Audio is already MP3, skipping conversion");
            return (true, inputBytes, null);
        }

        // WAV → MP3 conversion
        if (string.Equals(inputExtension, ".wav", StringComparison.OrdinalIgnoreCase))
        {
            return await ConvertWavToMp3Async(inputBytes, cancellationToken);
        }

        // Unknown format — try converting WAV by treating as raw
        _logger.LogWarning("Unknown audio format '{Ext}', attempting direct WAV conversion", inputExtension);
        return await ConvertWavToMp3Async(inputBytes, cancellationToken);
    }

    private async Task<(bool IsSuccess, byte[]? Mp3Bytes, string? ErrorMessage)> ConvertWavToMp3Async(
        byte[] wavBytes,
        CancellationToken cancellationToken)
    {
        var tempWav = Path.Combine(Path.GetTempPath(), $"tts_in_{Guid.NewGuid():N}.wav");
        var tempMp3 = Path.Combine(Path.GetTempPath(), $"tts_out_{Guid.NewGuid():N}.mp3");

        try
        {
            await File.WriteAllBytesAsync(tempWav, wavBytes, cancellationToken);

            // Try FFmpeg first (most reliable for .NET in Docker/Linux)
            var ffmpegResult = await TryFFmpegAsync(tempWav, tempMp3, cancellationToken);
            if (ffmpegResult.Success)
            {
                var mp3Bytes = await File.ReadAllBytesAsync(tempMp3, cancellationToken);
                _logger.LogInformation(
                    "FFmpeg conversion successful: {InSize} bytes WAV → {OutSize} bytes MP3",
                    wavBytes.Length, mp3Bytes.Length);
                return (true, mp3Bytes, null);
            }

            // Fallback: NAudio raw passthrough — AzuraCast may handle non-MP3
            _logger.LogWarning(
                "FFmpeg conversion failed ({Error}). Returning WAV bytes — AzuraCast may handle it directly.",
                ffmpegResult.ErrorMessage);
            return (true, wavBytes, ffmpegResult.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audio conversion failed");
            return (false, null, ex.Message);
        }
        finally
        {
            try { if (File.Exists(tempWav)) File.Delete(tempWav); } catch { /* ignore */ }
            try { if (File.Exists(tempMp3)) File.Delete(tempMp3); } catch { /* ignore */ }
        }
    }

    private async Task<(bool Success, string? ErrorMessage)> TryFFmpegAsync(
        string inputPath,
        string outputPath,
        CancellationToken cancellationToken)
    {
        try
        {
            // -y: overwrite output without asking
            // -codec:a libmp3lame: use LAME MP3 encoder
            // -q:a 2: quality level (0-9, lower = better quality, 2 ≈ 192-256kbps)
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
            if (process is null)
                return (false, "Could not start FFmpeg process");

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0 && File.Exists(outputPath))
                return (true, null);

            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            return (false, string.IsNullOrWhiteSpace(error) ? $"FFmpeg exited with code {process.ExitCode}" : error.Trim());
        }
        catch (FileNotFoundException)
        {
            return (false, "FFmpeg not found in PATH. Install FFmpeg to enable MP3 conversion.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
