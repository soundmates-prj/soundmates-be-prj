using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

using AiService.Application.Interfaces;

namespace AiService.Infrastructure.Services;

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
            var args = $"-y -i \"{inputPath}\" -codec:a libmp3lame -q:a 2 \"{outputPath}\"";
            return await RunFFmpegProcessAsync(args, cancellationToken);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool IsSuccess, byte[]? MixedBytes, string? ErrorMessage)> MixWithBackgroundMusicAsync(
        byte[] speechBytes,
        string speechExtension,
        string bgmUrlOrPath,
        CancellationToken cancellationToken = default)
    {
        var tempSpeech = Path.Combine(Path.GetTempPath(), $"tts_speech_{Guid.NewGuid():N}{speechExtension}");
        var tempBgm = Path.Combine(Path.GetTempPath(), $"tts_bgm_{Guid.NewGuid():N}.mp3");
        var tempOut = Path.Combine(Path.GetTempPath(), $"tts_mixed_{Guid.NewGuid():N}.mp3");

        try
        {
            await File.WriteAllBytesAsync(tempSpeech, speechBytes, cancellationToken);

            // Fetch BGM if it's a URL, otherwise assume local file
            if (bgmUrlOrPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                bgmUrlOrPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                using var http = new System.Net.Http.HttpClient();
                var bgmBytes = await http.GetByteArrayAsync(bgmUrlOrPath, cancellationToken);
                await File.WriteAllBytesAsync(tempBgm, bgmBytes, cancellationToken);
            }
            else
            {
                if (!File.Exists(bgmUrlOrPath))
                    return (false, null, $"Background music file not found: {bgmUrlOrPath}");
                File.Copy(bgmUrlOrPath, tempBgm);
            }

            // FFmpeg Ducking Mix:
            // [1:a]volume=0.2[bgm]: lowers BGM volume
            // amix=inputs=2:duration=first: mixes speech and lowered BGM, duration matches the speech length
            var filter = "\"[1:a]volume=0.3[bgm];[0:a][bgm]amix=inputs=2:duration=first:dropout_transition=2:weights=1 0.4[out]\"";
            var args = $"-y -i \"{tempSpeech}\" -i \"{tempBgm}\" -filter_complex {filter} -map \"[out]\" -codec:a libmp3lame -q:a 2 \"{tempOut}\"";

            var ffmpegResult = await RunFFmpegProcessAsync(args, cancellationToken);
            if (ffmpegResult.Success && File.Exists(tempOut))
            {
                var mixedBytes = await File.ReadAllBytesAsync(tempOut, cancellationToken);
                return (true, mixedBytes, null);
            }

            return (false, null, ffmpegResult.ErrorMessage ?? "FFmpeg mixing failed silently");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background music mixing failed");
            return (false, null, ex.Message);
        }
        finally
        {
            try { if (File.Exists(tempSpeech)) File.Delete(tempSpeech); } catch { }
            try { if (File.Exists(tempBgm)) File.Delete(tempBgm); } catch { }
            try { if (File.Exists(tempOut)) File.Delete(tempOut); } catch { }
        }
    }

    private async Task<(bool Success, string? ErrorMessage)> RunFFmpegProcessAsync(string args, CancellationToken cancellationToken)
    {
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

        if (process.ExitCode == 0)
            return (true, null);

        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        return (false, string.IsNullOrWhiteSpace(error) ? $"FFmpeg exited with code {process.ExitCode}" : error.Trim());
    }
}
