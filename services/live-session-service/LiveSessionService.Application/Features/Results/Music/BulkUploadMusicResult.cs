namespace LiveSessionService.Application.Features.Results.Music;

/// <summary>
/// Result model for bulk music upload operations
/// </summary>
public sealed class BulkUploadMusicResult
{
    /// <summary>
    /// Total number of files in the request
    /// </summary>
    public int TotalFiles { get; init; }

    /// <summary>
    /// Number of files successfully uploaded
    /// </summary>
    public int SuccessCount { get; init; }

    /// <summary>
    /// Number of files that failed to upload
    /// </summary>
    public int FailedCount { get; init; }

    /// <summary>
    /// List of successfully uploaded files
    /// </summary>
    public List<MusicResult> UploadedFiles { get; init; } = new();

    /// <summary>
    /// List of failed file uploads with error details
    /// </summary>
    public List<BulkUploadFailedItem> FailedFiles { get; init; } = new();

    /// <summary>
    /// Overall success status of the bulk operation
    /// </summary>
    public bool IsSuccess => FailedCount == 0;

    /// <summary>
    /// Overall status message
    /// </summary>
    public string Message =>
        FailedCount == 0
            ? $"Successfully uploaded {SuccessCount} file(s)."
            : $"Uploaded {SuccessCount}/{TotalFiles} file(s). {FailedCount} file(s) failed.";
}

/// <summary>
/// Details of a failed file upload in bulk operation
/// </summary>
public sealed class BulkUploadFailedItem
{
    /// <summary>
    /// Original file name that failed to upload
    /// </summary>
    public string FileName { get; init; } = null!;

    /// <summary>
    /// Error message describing why the upload failed
    /// </summary>
    public string ErrorMessage { get; init; } = null!;

    /// <summary>
    /// Index of the file in the original request (0-based)
    /// </summary>
    public int FileIndex { get; init; }
}
