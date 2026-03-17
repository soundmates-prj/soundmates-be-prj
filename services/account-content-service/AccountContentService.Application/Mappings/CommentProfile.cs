using AccountContentService.Application.DTOs;
using AccountContentService.Application.Features.BlogComments.Commands.CreateComment;
using AccountContentService.Application.Features.BlogComments.Commands.ReplyComment;
using AccountContentService.Application.Features.BlogComments.Commands.UpdateComment;
using AccountContentService.Application.Features.BlogPosts.Commands.UpdatePost;
using AccountContentService.Domain.Entities;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Mappings
{
    public class CommentProfile : Profile
    {
        public CommentProfile()
        {
            CreateMap<BlogComment, CommentDto>();
            CreateMap<CreateCommentCommand, BlogComment>();
            CreateMap<ReplyCommentCommand, BlogComment>();
            CreateMap<UpdateCommentCommand, BlogComment>()
                .ForAllMembers(opts =>
                opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
