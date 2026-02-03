namespace LiveSessionService.Application.Features.Common.AzuraCast.Models;

public sealed class AzuraCastStationListData
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Shortcode { get; init; }
    public string? Description { get; init; }
    public string? ListenUrl { get; init; }
    public string? PublicPlayerUrl { get; init; }
    public bool IsPublic { get; init; }
    public List<AzuraCastMountData>? Mounts { get; init; }
    public bool HlsEnabled { get; init; }
    public string? HlsUrl { get; init; }
}

public sealed class AzuraCastMountData
{
    public int Id { get; init; }
    public string? Name { get; init; }
    public string? Url { get; init; }
    public int? Bitrate { get; init; }
    public string? Format { get; init; }
    public AzuraCastListenersData? Listeners { get; init; }
    public string? Path { get; init; }
    public bool IsDefault { get; init; }
}
