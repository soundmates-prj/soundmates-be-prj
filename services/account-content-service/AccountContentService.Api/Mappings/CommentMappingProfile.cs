
using AccountContentService.Api.Contracts.Requests;
using AccountContentService.Api.Contracts.Responses;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Features.BlogComments.Commands.CreateComment;
using AccountContentService.Application.Features.BlogComments.Commands.ReplyComment;
using AccountContentService.Application.Features.BlogComments.Commands.UpdateComment;
using AccountContentService.Domain.Entities;
using AutoMapper;

namespace AccountContentService.Api.Mappings
{
    public class CommentMappingProfile : Profile
    {
        public CommentMappingProfile()
        {
            CreateMap<CommentRequest, CreateCommentCommand>();
            CreateMap<CommentRequest, ReplyCommentCommand>();
            CreateMap<CommentRequest, UpdateCommentCommand>();
            CreateMap<CommentDto, CommentResponse>();
        }
    }
}
