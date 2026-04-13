namespace LiveSessionService.Domain.Entities;

/// <summary>
/// Audit log for sync operations
/// </summary>
public class SyncAuditLog
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Type of sync: Station, MediaFile, Playlist
    /// </summary>
    public string SyncType { get; set; } = null!;
    
    /// <summary>
    /// Station ID if applicable
    /// </summary>
    public Guid? StationId { get; set; }
    
    /// <summary>
    /// User who triggered the sync (or system user)
    /// </summary>
    public Guid TriggeredByUserId { get; set; }
    
    /// <summary>
    /// Sync start time
    /// </summary>
    public DateTime StartedAt { get; set; }
    
    /// <summary>
    /// Sync end time
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Status: InProgress, Completed, Failed, PartialSuccess
    /// </summary>
    public string Status { get; set; } = "InProgress";
    
    /// <summary>
    /// Total records processed
    /// </summary>
    public int TotalRecords { get; set; }
    
    /// <summary>
    /// Records created
    /// </summary>
    public int CreatedRecords { get; set; }
    
    /// <summary>
    /// Records updated
    /// </summary>
    public int UpdatedRecords { get; set; }
    
    /// <summary>
    /// Records deleted
    /// </summary>
    public int DeletedRecords { get; set; }
    
    /// <summary>
    /// Records failed
    /// </summary>
    public int FailedRecords { get; set; }
    
    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Detailed error log (JSON)
    /// </summary>
    public string? ErrorDetails { get; set; }
    
    /// <summary>
    /// Duration in seconds
    /// </summary>
    public int DurationSeconds { get; set; }
}
