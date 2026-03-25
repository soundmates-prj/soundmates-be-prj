using AccountContentService.Application.DTOs;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using AccountContentService.Domain.Entities;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace AccountContentService.Application.Features.BlogPostReactions.Commands.CreateReaction
{
    public class CreateReactionHandler : IRequestHandler<CreateReactionCommand, ReactionDto>
    {
        private readonly IPostReactionRepository _repository;
        private readonly IUserServiceClient _userServiceClient;
        private readonly IMapper _mapper;

        public CreateReactionHandler(IPostReactionRepository repository, IUserServiceClient userServiceClient, IMapper mapper)
        {
            _repository = repository;
            _userServiceClient = userServiceClient;
            _mapper = mapper;
        }

        public async Task<ReactionDto> Handle(CreateReactionCommand request, CancellationToken cancellationToken)
        {
            var user = await _userServiceClient.GetMyProfile() ?? throw new Exception("User is not available!");

            var reaction = _mapper.Map<PostReaction>(request);
            reaction.CreatedAt = DateTime.UtcNow;
            reaction.UserAvatarUrl = user.ProfileImageUrl ?? string.Empty;
            reaction.UserFullName = $"{user.FirstName ?? ""} {user.LastName ?? ""}".Trim() ?? string.Empty;

            await _repository.AddAsync(reaction);

            return _mapper.Map<ReactionDto>(reaction);
        }
    }
}
