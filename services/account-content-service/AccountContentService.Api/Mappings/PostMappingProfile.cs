using AccountContentService.Api.Common;
using AccountContentService.Api.Contracts.Requests;
using AccountContentService.Api.Contracts.Responses;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Features.BlogPosts.Commands.CreatePost;
using AccountContentService.Application.Features.BlogPosts.Commands.UpdatePost;
using AccountContentService.Application.Features.BlogPosts.Queries.GetPosts;
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
            CreateMap<PostDto, PostResponse>();
            CreateMap<CreatePostRequest, CreatePostCommand>();
            CreateMap<UpdatePostRequest, UpdatePostCommand>();
        }
    }
}
