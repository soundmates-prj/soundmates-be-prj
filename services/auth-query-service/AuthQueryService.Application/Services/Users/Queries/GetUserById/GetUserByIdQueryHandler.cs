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
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            };
            return ApiResponse<UserReadDto>.SuccessResponse(dto);
        }
    }
}