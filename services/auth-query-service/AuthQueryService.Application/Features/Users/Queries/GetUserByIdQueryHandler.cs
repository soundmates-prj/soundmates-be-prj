using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.DTOs;
using AuthQueryService.Application.DTOs.Response;
using AuthQueryService.Domain.Interfaces;

namespace AuthQueryService.Application.Services.Users.Queries.GetUserById
{
    public sealed class GetUserByIdQueryHandler
        : IQueryHandler<GetUserByIdQuery, UserReadDto>
    {
        private readonly IUserReadRepository _repository;

        public GetUserByIdQueryHandler(IUserReadRepository repository) => _repository = repository;

        public async Task<ApiResponse<UserReadDto>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
        {
            var u = await _repository.GetByIdAsync(query.Id);
            if (u is null) return ApiResponse<UserReadDto>.FailureResponse("User not found", 404);

            var dto = new UserReadDto
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
            };
            return ApiResponse<UserReadDto>.SuccessResponse(dto);
        }
    }
}