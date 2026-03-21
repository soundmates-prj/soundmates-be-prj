using AccountContentService.Api.Common;
using AccountContentService.Api.Contracts.Requests;
using AccountContentService.Api.Contracts.Responses;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Features.BlogPosts.Commands.CreatePost;
using AccountContentService.Application.Features.BlogPosts.Commands.ShareMusicPost;
using AccountContentService.Application.Features.BlogPosts.Commands.UpdatePost;
using AccountContentService.Application.Features.BlogPosts.Queries.GetPopularPosts;
using AccountContentService.Application.Features.BlogPosts.Queries.GetPosts;
using AccountContentService.Application.Features.BlogPosts.Queries.GetPostStats;
using AccountContentService.Application.Features.BlogPosts.Queries.GetTrendingPosts;
using AccountContentService.Domain.Entities;
using AutoMapper;

namespace AccountContentService.Api.Mappings
{
    public class PostMappingProfile : Profile
    {
        public PostMappingProfile()
        {
            CreateMap<BlogPost, CreatePostRequest>();
            CreateMap <PaginationRequest, GetPostsQuery>();
            CreateMap <PaginationRequest, GetPublisedPostsQuery>();
            CreateMap <PaginationRequest, GetTrendingPostsQuery>();
            CreateMap <PaginationRequest, GetPopularPostsQuery>();
            CreateMap <PaginationRequest, GetPostsStatsQuery>();
            CreateMap<PostDto, PostResponse>();
            CreateMap<ShareMusicDto, ShareMusicResponse>();
            CreateMap<CreatePostRequest, CreatePostCommand>();
            CreateMap<ShareMusicPostRequest, ShareMusicPostCommand>();
            CreateMap<UpdatePostRequest, UpdatePostCommand>();
        }
    }
}
