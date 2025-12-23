using AuthQueryService.Domain.Entities;
using System;
using System.Data;

namespace AuthQueryService.Application.DTOs
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
        public string? Token { get; set; } // JWT token property added
        public DateTime? CreatedAt { get; set; }

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
            CreatedAt = user.CreatedAt;
        }
        public User ToEntity() => new User
        {
            Id = Id == Guid.Empty ? Guid.NewGuid() : Id,
            Username = Username,
            Email = Email,
            FirstName = FirstName,
            LastName = LastName,
            RoleId = RoleId,
            CreatedAt = CreatedAt
        };
    }
}
