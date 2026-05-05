using AccountContentService.Application.DTOs;
using AccountContentService.Application.Features.BlogPosts.Commands.CreatePost;
using AccountContentService.Application.Features.BlogPosts.Commands.UpdatePost;
using AccountContentService.Domain.Entities;
using AutoMapper;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Mappings
{
    public class BlogPostProfile : Profile
    {
        public BlogPostProfile()
        {
            CreateMap<BlogPost, PostDto>()
                .ForMember(dest => dest.PostType, opt => opt.MapFrom(src =>
                    src.MoodTag != null && src.MoodTag.StartsWith("share-music:") ? "share-music" : null))
                .ForMember(dest => dest.ShareMusic, opt => opt.MapFrom(src => BuildShareMusic(src)));
            CreateMap<CreatePostCommand, BlogPost>();
            CreateMap<UpdatePostCommand, BlogPost>()
                .ForAllMembers(opts =>
                opts.Condition((src, dest, srcMember) => srcMember != null));
            CreateMap<BlogReport, BlogReportDto>();
        }

        private static ShareMusicDto? BuildShareMusic(BlogPost src)
        {
            if (src.MoodTag == null || !src.MoodTag.StartsWith("share-music:"))
            {
                return null;
            }

            try
            {
                var parsed = JsonSerializer.Deserialize<ShareMusicDto>(src.ContentText);
                if (parsed == null)
                {
                    return null;
                }

                if (string.IsNullOrWhiteSpace(parsed.Template))
                {
                    parsed.Template = src.MoodTag.Replace("share-music:", string.Empty).Trim();
                }

                return parsed;
            }
            catch
            {
                return new ShareMusicDto
                {
                    TrackId = string.Empty,
                    Title = src.Title,
                    Artist = string.Empty,
                    AlbumImage = src.ImageUrl ?? string.Empty,
                    PreviewUrl = src.AudioUrl,
                    Template = src.MoodTag.Replace("share-music:", string.Empty).Trim()
                };
            }
        }
    }
}
