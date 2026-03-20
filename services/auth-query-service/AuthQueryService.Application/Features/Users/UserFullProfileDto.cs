namespace AuthQueryService.Application.DTOs
{
    public sealed class UserFullProfileDto
    {
        // Account Information
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
        
        // Profile Information
        public string? Bio { get; init; }
        public string? ProfileImageUrl { get; init; }
        public string? BackgroundImageUrl { get; init; }
        public string? Phone { get; init; }
        public string? Gender { get; init; }
        public DateTime? DateOfBirth { get; init; }
        public string? Location { get; init; }
        public string? Website { get; init; }
    }
}
