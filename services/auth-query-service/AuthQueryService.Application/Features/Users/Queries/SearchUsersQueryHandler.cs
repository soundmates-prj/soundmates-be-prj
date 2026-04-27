using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.DTOs;
using AuthQueryService.Application.DTOs.Response;
using AuthQueryService.Domain.Interfaces;

namespace AuthQueryService.Application.Services.Users.Queries.SearchUsers
{
    public sealed class SearchUsersQueryHandler
        : IQueryHandler<SearchUsersQuery, PagedResult<UserReadDto>>
    {
        private readonly IUserReadRepository _repository;

        public SearchUsersQueryHandler(IUserReadRepository repository) => _repository = repository;

        public async Task<ApiResponse<PagedResult<UserReadDto>>> Handle(SearchUsersQuery query, CancellationToken cancellationToken)
        {
            var page = query.Page <= 0 ? 1 : query.Page;
            var size = query.PageSize <= 0 ? 20 : query.PageSize;

            var (items, total) = await _repository.SearchAsync(query.Q, page, size);

            var dtoItems = items.Select(x => new UserReadDto
            {
                Id = x.Id,
                Username = x.Username,
                Email = x.Email,
                FirstName = x.FirstName,
                LastName = x.LastName,
                RoleId = x.RoleId,
                RoleName = x.RoleName,
                AccountStatus = x.AccountStatus,
                IsActive = x.IsActive,
                IsVerified = x.IsVerified,
                EmailVerifiedAt = x.EmailVerifiedAt,
                IsBanned = x.IsBanned,
                BannedAt = x.BannedAt,
                BanReason = x.BanReason,
                DeactivatedAt = x.DeactivatedAt,
                DeactivationReason = x.DeactivationReason,
                DeletionRequestedAt = x.DeletionRequestedAt,
                DeletionScheduledAt = x.DeletionScheduledAt,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            }).ToList();

            var result = new PagedResult<UserReadDto>
            {
                Items = dtoItems,
                Page = page,
                PageSize = size,
                TotalItems = total
            };

            return ApiResponse<PagedResult<UserReadDto>>.SuccessResponse(result);
        }
    }
}