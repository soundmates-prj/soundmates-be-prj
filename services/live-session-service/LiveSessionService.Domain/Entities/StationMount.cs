using System;

namespace LiveSessionService.Domain.Entities;

public class StationMount
{
    public Guid Id { get; set; }
    
    public Guid AzuraCastStationId { get; set; }
    
    public int ExternalMountId { get; set; }
    
    public string MountName { get; set; } = null!;
    
    public string MountPath { get; set; } = null!;
    
    public string? MountUrl { get; set; }
    
    public bool IsDefault { get; set; }
    
    public bool IsPublic { get; set; } = true;
    
    public int? Bitrate { get; set; }
    
    public string? Format { get; set; }
    
    public int? CurrentListeners { get; set; }
    
    public int? UniqueListeners { get; set; }
    
    public bool IsEnabled { get; set; } = true;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public virtual AzuraCastStation AzuraCastStation { get; set; } = null!;
}
