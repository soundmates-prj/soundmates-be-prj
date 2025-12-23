using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.DTOs;
using System;

namespace AuthService.Application.Services.Auth.Commands
{
    public sealed record UpdateProfileOptionsCommand : ICommand<UserDto>
    {
        public Guid UserId { get; init; }
        public string? Bio { get; init; }
        public string? Phone { get; init; }
        public string? Gender { get; init; }
        public DateTime? DateOfBirth { get; init; }
        public string? ProfileImageUrl { get; init; }
        public string? BackgroundImageUrl { get; init; }
        public string? Location { get; init; }
        public string? Website { get; init; }
    }
}

