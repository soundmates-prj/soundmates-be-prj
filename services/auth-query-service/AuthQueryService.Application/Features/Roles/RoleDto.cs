using AuthQueryService.Domain.Entities.ReadModels;

namespace AuthQueryService.Application.DTOs
{
    /// <summary>
    /// Data Transfer Object for Role information
    /// Maps from RoleReadModel to API response
    /// </summary>
    public sealed class RoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }

        public RoleDto() { }
        
        public RoleDto(RoleReadModel role)
        {
            Id = role.Id;
            Name = role.Name;
            Description = role.Description;
            CreatedAt = role.CreatedAt;
        }
    }
}
