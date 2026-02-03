using System;

namespace AuthService.Application.Results
{
    /// <summary>
    /// User Result for Application layer
    /// Pure data contract - no Domain dependencies
    /// Used for user profile information (NOT for auth responses)
    /// </summary>
    public class UserResult
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public Guid? RoleId { get; set; }
        public string? RoleName { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? EmailVerifiedAt { get; set; }
    }
}
