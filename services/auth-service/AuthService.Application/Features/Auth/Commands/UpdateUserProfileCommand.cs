using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Results;

namespace AuthService.Application.Features.Auth.Commands;

/// <summary>
/// Update user profile command - updates both basic user info (firstname, lastname) 
/// and extended profile fields (bio, phone, etc.)
/// All fields are nullable - only provided fields will be updated
/// </summary>
public sealed record UpdateUserProfileCommand : ICommand<UserProfileResult>
{
    public required Guid UserId { get; init; }
    
    // Basic user info (optional)
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    
    // Extended profile fields (all optional)
    public string? Bio { get; init; }
    public string? Phone { get; init; }
    public string? Gender { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public string? ProfileImageUrl { get; init; }
    public string? BackgroundImageUrl { get; init; }
    public string? Location { get; init; }
    public string? Website { get; init; }
}
