using System.Text.Json;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using AccountContentService.Domain.Enums;
using MediatR;

namespace AccountContentService.Application.Features.BlogPosts.Commands.ShareMusicPost;

public class ShareMusicPostHandler : IRequestHandler<ShareMusicPostCommand, PostDto>
{
    private readonly IBlogPostRepository _postRepository;

    public ShareMusicPostHandler(IBlogPostRepository postRepository)
    {
        _postRepository = postRepository;
    }

    public async Task<PostDto> Handle(ShareMusicPostCommand request, CancellationToken cancellationToken)
    {
        var payload = new ShareMusicDto
        {
            TrackId = request.TrackId,
            Title = request.Title,
            Artist = request.Artist,
            AlbumImage = request.AlbumImage,
            PreviewUrl = request.PreviewUrl,
            Template = request.Template
        };

        var post = new BlogPost
        {
            UserId = request.UserId,
            Title = request.Title,
            ContentText = JsonSerializer.Serialize(payload),
            ImageUrl = request.AlbumImage,
            AudioUrl = request.PreviewUrl,
            PrivacyScope = "Public",
            MoodTag = $"share-music:{request.Template.ToLowerInvariant()}",
            Status = PostStatus.Published.ToString(),
            PublishedAt = DateTime.UtcNow
        };

        await _postRepository.AddAsync(post);

        return new PostDto
        {
            Id = post.Id,
            UserId = post.UserId,
            Title = post.Title,
            ContentText = post.ContentText,
            AudioUrl = post.AudioUrl,
            ImageUrl = post.ImageUrl,
            IsActive = post.IsActive,
            PrivacyScope = post.PrivacyScope,
            MoodTag = post.MoodTag,
            Status = post.Status,
            IsGenerated = post.IsGenerated,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            PublishedAt = post.PublishedAt,
            PostType = "share-music",
            ShareMusic = payload
        };
    }
}
