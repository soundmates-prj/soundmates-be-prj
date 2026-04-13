namespace LiveSessionService.Domain.Entities;

/// <summary>
/// Many-to-many mapping between MediaFile and AzuraCastStation.
/// Tracks which system media files have been imported to which stations.
/// </summary>
public class StationMediaFile
{
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the MediaFile (system or station music).
    /// </summary>
    public Guid MediaFileId { get; set; }

    /// <summary>
    /// Reference to the AzuraCast station.
    /// </summary>
    public Guid StationId { get; set; }

    /// <summary>
    /// AzuraCast unique_id for this media on this specific station.
    /// The same MediaFile can have different UniqueIds on different stations.
    /// </summary>
    public string AzuraCastMediaId { get; set; } = null!;

    /// <summary>
    /// When this media was imported to this station.
    /// </summary>
    public DateTime ImportedAt { get; set; }

    // Navigation properties
    public virtual MediaFile MediaFile { get; set; } = null!;
    public virtual AzuraCastStation Station { get; set; } = null!;
}
