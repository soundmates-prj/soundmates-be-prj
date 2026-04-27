using AiService.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace AiService.Infrastructure.Storage;

public class LocalAudioStorage : IAudioStorage
{
    private readonly string _audioRoot;

    public LocalAudioStorage(IOptions<StorageOptions> options)
    {
        _audioRoot = options.Value.AudioRoot?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_audioRoot) || _audioRoot.StartsWith("${"))
        {
            _audioRoot = Path.Combine(AppContext.BaseDirectory, "data", "audios");
        }

        Directory.CreateDirectory(_audioRoot);
    }

    public async Task<StoredAudioFile> SaveAsync(
        string fileNameWithoutExtension,
        string extensionWithDot,
        string contentType,
        byte[] bytes,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
            throw new ArgumentException("file name is required", nameof(fileNameWithoutExtension));
        if (string.IsNullOrWhiteSpace(extensionWithDot) || !extensionWithDot.StartsWith('.'))
            throw new ArgumentException("extension must start with '.'", nameof(extensionWithDot));

        var safeName = string.Concat(fileNameWithoutExtension.Where(char.IsLetterOrDigit));
        if (safeName.Length == 0)
            safeName = Guid.NewGuid().ToString("N");

        var relative = $"{safeName}{extensionWithDot}";
        var fullPath = Path.Combine(_audioRoot, relative);

        var tempPath = Path.Combine(_audioRoot, $".{safeName}.{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllBytesAsync(tempPath, bytes, cancellationToken);
            File.Move(tempPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }

        var fi = new FileInfo(fullPath);
        return new StoredAudioFile(RelativePath: relative, SizeBytes: fi.Length, ContentType: contentType);
    }

    public Task<(Stream Stream, string ContentType, long? ContentLength)> OpenReadAsync(string relativePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("relativePath is required", nameof(relativePath));

        var safe = relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        if (safe.Contains("..", StringComparison.Ordinal))
            throw new ArgumentException("invalid path", nameof(relativePath));

        var fullPath = Path.Combine(_audioRoot, safe);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("audio file not found", fullPath);

        // Open as async-capable stream so ASP.NET can stream efficiently (CopyToAsync, range reads, etc.).
        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".wav" => "audio/wav",
            ".mp3" => "audio/mpeg",
            _ => "application/octet-stream"
        };
        return Task.FromResult<(Stream, string, long?)>((stream, contentType, stream.Length));
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return Task.CompletedTask;

        var safe = relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_audioRoot, safe);
        
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        
        return Task.CompletedTask;
    }
}

