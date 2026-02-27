namespace LiveSessionService.Application.Features.Common.AzuraCast.Models;

public sealed class AzuraCastStationData
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public string? ShortCode { get; init; }
    public string? Description { get; init; }
    public string? ListenUrl { get; init; }
    public string? PublicPlayerUrl { get; init; }
    public bool IsPublic { get; init; }
    public bool HlsEnabled { get; init; }
    public string? HlsUrl { get; init; }
    public List<AzuraCastMountData>? Mounts { get; init; }
}
