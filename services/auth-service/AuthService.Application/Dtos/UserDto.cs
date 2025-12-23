using AuthService.Domain.Entities;
using System;
using System.Data;

namespace AuthService.Application.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public Guid? RoleId { get; set; }
        public string? RoleName { get; set; }

        public string? Password { get; set; } // Plaintext password property for creation/update
        public string? Token { get; set; } // JWT access token property
        public string? RefreshToken { get; set; } // Refresh token property
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Added: parameterless constructor to allow object initializer usage (e.g., new UserDto { ... })
        public UserDto() { }

        public UserDto(User user)
        {
            Id = user.Id;
            Username = user.Username;
            Email = user.Email;
            FirstName = user.FirstName;
            LastName = user.LastName;
            RoleId = user.RoleId;               // FIX: populate RoleId
            RoleName = user.Role?.Name;         // Requires Role to be loaded
            IsActive = user.IsActive;
            CreatedAt = user.CreatedAt;
            UpdatedAt = user.UpdatedAt;
        }
        
        public User ToEntity() => new User
        {
            Id = Id == Guid.Empty ? Guid.NewGuid() : Id,
            Username = Username,
            Email = Email,
            FirstName = FirstName,
            LastName = LastName,
            RoleId = RoleId,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
    }
}
