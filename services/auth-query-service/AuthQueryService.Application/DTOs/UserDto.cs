namespace AuthQueryService.Application.DTOs
{
    /// <summary>
    /// Basic User DTO for API responses
    /// Note: In Query Service, DTOs are populated from Read Models, not Domain Entities
    /// </summary>
    public sealed class UserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public Guid? RoleId { get; set; }
        public string? RoleName { get; set; }
        public string? Token { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
