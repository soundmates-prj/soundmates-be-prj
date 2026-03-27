using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Mappings;
using AuthService.Application.Results;
using AuthService.Application.Features.Auth.Commands;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;
using Shared.Contracts.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Features.Auth.Handlers;

/// <summary>
/// Handler for updating user profile (both basic info and extended profile)
/// Supports partial updates - only updates fields that are provided
/// </summary>
public sealed class UpdateUserProfileHandler : ICommandHandler<UpdateUserProfileCommand, UserProfileResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly IOutboxRepository _outbox;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateUserProfileHandler(
        IUserRepository userRepository,
        IProfileRepository profileRepository,
        IOutboxRepository outbox,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _userRepository = userRepository;
        _profileRepository = profileRepository;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<UserProfileResult>> Handle(UpdateUserProfileCommand command, CancellationToken cancellationToken)
    {
        // Get user
        var user = await _userRepository.GetByIdAsync(command.UserId);
        if (user == null)
        {
            return Result<UserProfileResult>.Failure("User not found", 404);
        }

        var hasUserUpdate = false;
        var hasProfileUpdate = false;

        // Update basic user info if provided (firstname, lastname)
        if (!string.IsNullOrWhiteSpace(command.FirstName) || !string.IsNullOrWhiteSpace(command.LastName))
        {
            try
            {
                user.UpdateName(
                    command.FirstName ?? user.FirstName,
                    command.LastName ?? user.LastName,
                    _dateTimeProvider);
                hasUserUpdate = true;
            }
            catch (UserValidationException ex)
            {
                return Result<UserProfileResult>.Failure(ex.Message, ex.StatusCode);
            }
        }

        // Get or create profile
        var profile = await _profileRepository.GetByUserIdAsync(command.UserId);
        if (profile == null)
        {
            profile = new Profile
            {
                Id = Guid.NewGuid(),
                UserId = command.UserId,
                CreatedAt = _dateTimeProvider.UtcNow,
                UpdatedAt = _dateTimeProvider.UtcNow
            };
            await _profileRepository.CreateAsync(profile);
            hasProfileUpdate = true;
        }

        // Update profile fields if provided (only update non-null fields)
        if (command.Bio != null)
        {
            profile.Bio = command.Bio.Trim();
            hasProfileUpdate = true;
        }

        if (command.Phone != null)
        {
            profile.Phone = command.Phone.Trim();
            hasProfileUpdate = true;
        }

        if (command.Gender != null)
        {
            var genderValue = command.Gender.Trim();
            // Validate against Gender enum (Male or Female only)
            if (Enum.TryParse<Gender>(genderValue, true, out var gender))
            {
                profile.Gender = gender.ToString();
                hasProfileUpdate = true;
            }
        }

        if (command.DateOfBirth.HasValue)
        {
            profile.DateOfBirth = command.DateOfBirth.Value;
            hasProfileUpdate = true;
        }

        if (command.ProfileImageUrl != null)
        {
            profile.ProfileImageUrl = command.ProfileImageUrl.Trim();
            hasProfileUpdate = true;
        }

        if (command.BackgroundImageUrl != null)
        {
            profile.BackgroundImageUrl = command.BackgroundImageUrl.Trim();
            hasProfileUpdate = true;
        }

        if (command.Location != null)
        {
            profile.Location = command.Location.Trim();
            hasProfileUpdate = true;
        }

        if (command.Website != null)
        {
            profile.Website = command.Website.Trim();
            hasProfileUpdate = true;
        }

        // Update timestamps if there were changes
        if (hasProfileUpdate)
        {
            profile.UpdatedAt = _dateTimeProvider.UtcNow;
            await _profileRepository.UpdateAsync(profile);
        }

        if (hasUserUpdate)
        {
            await _userRepository.UpdateAsync(user);
        }

        // Commit transaction if any updates occurred
        if (hasUserUpdate || hasProfileUpdate)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Reload user with role
        user = await _userRepository.GetByIdAsync(command.UserId);
        profile = await _profileRepository.GetByUserIdAsync(command.UserId);

        // Publish typed UserProfileUpdatedEvent to Outbox
        var evt = new UserProfileUpdatedEvent
        {
            Id = user!.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            RoleId = user.RoleId ?? Guid.Empty,
            RoleName = user.Role?.Name,
            Profile = new UserProfileDetail
            {
                Bio = profile?.Bio,
                Phone = profile?.Phone,
                Gender = profile?.Gender,
                DateOfBirth = profile?.DateOfBirth,
                ProfileImageUrl = profile?.ProfileImageUrl,
                BackgroundImageUrl = profile?.BackgroundImageUrl,
                Location = profile?.Location,
                Website = profile?.Website
            },
            UpdatedAt = user.UpdatedAt ?? _dateTimeProvider.UtcNow
        };
        await _outbox.EnqueueAsync("auth.user.profile.updated", evt, cancellationToken);

        // Map to UserProfileResult
        var result = user.ToProfileResult(profile);

        return Result<UserProfileResult>.Success(result, "Profile updated successfully");
    }
}
