using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.DTOs;
using AuthQueryService.Application.DTOs.Response;
using AuthQueryService.Domain.Interfaces;

namespace AuthQueryService.Application.Roles.Queries.SearchRoles
{
    public sealed class SearchRolesQueryHandler : IQueryHandler<SearchRolesQuery, PagedResult<RoleDto>>
    {
        private readonly IRoleRepository _repository;

        public SearchRolesQueryHandler(IRoleRepository repository) => _repository = repository;

        public async Task<ApiResponse<PagedResult<RoleDto>>> Handle(SearchRolesQuery query, CancellationToken cancellationToken)
        {
            var page = query.Page <= 0 ? 1 : query.Page;
            var size = query.PageSize <= 0 ? 20 : query.PageSize;

            var allRoles = await _repository.GetAllAsync();
            
            // Filter by search term if provided
            var filteredRoles = allRoles;
            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var searchTerm = query.Q.ToLowerInvariant();
                filteredRoles = allRoles
                    .Where(r => r.Name.ToLowerInvariant().Contains(searchTerm))
                    .ToList();
            }

            var total = filteredRoles.Count;
            var items = filteredRoles
                .Skip((page - 1) * size)
                .Take(size)
                .Select(r => new RoleDto(r))
                .ToList();

            var result = new PagedResult<RoleDto>
            {
                Items = items,
                Page = page,
                PageSize = size,
                TotalItems = total
            };

            return ApiResponse<PagedResult<RoleDto>>.SuccessResponse(result);
        }
    }
}

