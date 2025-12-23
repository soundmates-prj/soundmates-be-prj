using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.DTOs;
using AuthQueryService.Application.DTOs.Response;
using AuthQueryService.Domain.Interfaces;

namespace AuthQueryService.Application.Users.Queries.GetUserByUsername
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
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            });
        }
    }
}