using System;
using System.Threading;
using System.Threading.Tasks;

namespace AccountContentService.Application.Interfaces.Services;

public interface ILiveSessionApiClient
{
    Task<PodcastDto?> GetPodcastAsync(Guid podcastId, CancellationToken cancellationToken);
}

public class PodcastDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsPaid { get; set; }
    public Guid CreatedBy { get; set; }
}
