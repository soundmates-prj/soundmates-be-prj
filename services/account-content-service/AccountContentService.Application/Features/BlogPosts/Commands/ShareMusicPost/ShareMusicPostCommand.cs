using AccountContentService.Application.DTOs;
using MediatR;

namespace AccountContentService.Application.Features.BlogPosts.Commands.ShareMusicPost;

public class ShareMusicPostCommand : IRequest<PostDto>
{
    public Guid UserId { get; set; }
    public required string TrackId { get; set; }
    public required string Title { get; set; }
    public required string Artist { get; set; }
    public required string AlbumImage { get; set; }
    public string? PreviewUrl { get; set; }
    public required string Template { get; set; }
}
