using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.DTOs;
using AuthQueryService.Application.DTOs.Response;
using AuthQueryService.Domain.Interfaces;

namespace AuthQueryService.Application.Services.Users.Queries.GetUserByUsername
{
    public sealed class GetUserByUsernameQueryHandler
        : IQueryHandler<GetUserByUsernameQuery, UserReadDto>
    {
        private readonly IUserReadRepository _repository;

        public GetUserByUsernameQueryHandler(IUserReadRepository repository) => _repository = repository;

        public async Task<ApiResponse<UserReadDto>> Handle(GetUserByUsernameQuery query, CancellationToken cancellationToken)
        {
            var u = await _repository.GetByUsernameAsync(query.Username);
            if (u is null) 
                return ApiResponse<UserReadDto>.FailureResponse("User not found", 404);

            return ApiResponse<UserReadDto>.SuccessResponse(new UserReadDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                RoleId = u.RoleId,
                RoleName = u.RoleName,
                AccountStatus = u.AccountStatus,
                IsActive = u.IsActive,
                IsVerified = u.IsVerified,
                EmailVerifiedAt = u.EmailVerifiedAt,
                IsBanned = u.IsBanned,
                BannedAt = u.BannedAt,
                BanReason = u.BanReason,
                DeactivatedAt = u.DeactivatedAt,
                DeactivationReason = u.DeactivationReason,
                DeletionRequestedAt = u.DeletionRequestedAt,
                DeletionScheduledAt = u.DeletionScheduledAt,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            });
        }
    }
}