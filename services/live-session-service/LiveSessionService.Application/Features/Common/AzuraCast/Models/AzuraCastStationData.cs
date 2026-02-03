namespace LiveSessionService.Application.Features.Common.AzuraCast.Models;

public sealed class AzuraCastStationData
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public string? ShortCode { get; init; }
    public AzuraCastListenersData? Listeners { get; init; }
}
