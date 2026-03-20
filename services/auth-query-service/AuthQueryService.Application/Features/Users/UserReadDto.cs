namespace AuthQueryService.Application.DTOs
{
    public sealed class UserReadDto
    {
        public Guid Id { get; init; }
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public Guid? RoleId { get; init; }
        public string? RoleName { get; init; }
        public bool IsActive { get; init; }
        public DateTime? CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
    }
}