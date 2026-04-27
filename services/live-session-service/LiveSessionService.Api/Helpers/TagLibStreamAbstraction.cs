namespace LiveSessionService.Api.Helpers;

/// <summary>
/// Allows TagLib# to read ID3/Vorbis tags directly from an in-memory stream
/// without requiring a file path on disk.
/// </summary>
internal sealed class TagLibStreamAbstraction : TagLib.File.IFileAbstraction
{
    private readonly Stream _stream;

    public TagLibStreamAbstraction(Stream stream, string fileName)
    {
        _stream = stream;
        Name    = fileName;
    }

    public string Name { get; }
    public Stream ReadStream  => _stream;
    public Stream WriteStream => _stream;

    /// <summary>Do not close — the caller owns and manages the stream lifetime.</summary>
    public void CloseStream(Stream stream) { }
}
