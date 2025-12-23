using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.DTOs;
using AuthQueryService.Application.DTOs.Response;
using AuthQueryService.Domain.Interfaces;

namespace AuthQueryService.Application.Users.Queries.GetFullUserProfile
{
    public sealed class GetFullUserProfileQueryHandler
        : IQueryHandler<GetFullUserProfileQuery, UserFullProfileDto>
    {
        private readonly IUserReadRepository _repository;

        public GetFullUserProfileQueryHandler(IUserReadRepository repository) => _repository = repository;

        public async Task<ApiResponse<UserFullProfileDto>> Handle(GetFullUserProfileQuery query, CancellationToken cancellationToken)
        {
            var user = await _repository.GetByIdAsync(query.UserId);
            if (user is null) 
                return ApiResponse<UserFullProfileDto>.FailureResponse("User not found", 404);

            var dto = new UserFullProfileDto
            {
                // Account Information
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                RoleId = user.RoleId,
                RoleName = user.RoleName,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                
                // Profile Information
                Bio = user.Bio,
                ProfileImageUrl = user.ProfileImageUrl,
                BackgroundImageUrl = user.BackgroundImageUrl,
                Phone = user.Phone,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                Location = user.Location,
                Website = user.Website
            };
            
            return ApiResponse<UserFullProfileDto>.SuccessResponse(dto);
        }
    }
}
