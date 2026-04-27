using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Music;

namespace LiveSessionService.Application.Features.Music.Commands.BulkUploadMusic;

/// <summary>
/// Command for bulk uploading multiple music files in a single request
/// </summary>
public sealed record BulkUploadMusicCommand(
    /// <summary>
    /// Optional station ID context for the upload
    /// </summary>
    Guid? StationId,

    /// <summary>
    /// User ID of the uploader
    /// </summary>
    Guid UploadedByUserId,

    /// <summary>
    /// List of files to upload, each with its metadata
    /// </summary>
    IReadOnlyList<BulkUploadFileEntry> Files) : ICommand<BulkUploadMusicResult>;

/// <summary>
/// Represents a single file entry in bulk upload
/// </summary>
public sealed record BulkUploadFileEntry(
    /// <summary>
    /// File content as a stream
    /// </summary>
    Stream FileStream,

    /// <summary>
    /// Original file name
    /// </summary>
    string FileName,

    /// <summary>
    /// Content type of the file
    /// </summary>
    string ContentType,

    /// <summary>
    /// Optional title (auto-detected from tags if not provided)
    /// </summary>
    string? Title,

    /// <summary>
    /// Optional artist (auto-detected from tags if not provided)
    /// </summary>
    string? Artist,

    /// <summary>
    /// Optional album (auto-detected from tags if not provided)
    /// </summary>
    string? Album
);
