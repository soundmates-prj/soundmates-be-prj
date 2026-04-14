using Xunit;
using Moq;
using FluentAssertions;
using System;
using System.Threading;
using System.Threading.Tasks;
using AuthService.Application.Features.Auth.Handlers;
using AuthService.Application.Features.Auth.Commands;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Application.Results;
using Shared.Contracts;
using Shared.Contracts.Events.Auth;

public class UpdateUserProfileHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IProfileRepository> _profileRepo = new();
    private readonly Mock<IOutboxRepository> _outbox = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IDateTimeProvider> _date = new();

    private UpdateUserProfileHandler CreateHandler()
    {
        return new UpdateUserProfileHandler(
            _userRepo.Object,
            _profileRepo.Object,
            _outbox.Object,
            _uow.Object,
            _date.Object
        );
    }


    //USER NOT FOUND

    [Fact]
    public async Task ShouldFail_WhenUserNotFound()
    {
        var handler = CreateHandler();

        _userRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((User?)null);

        var result = await handler.Handle(new UpdateUserProfileCommand
        {
            UserId = Guid.NewGuid()
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("User not found");
    }


    //CREATE PROFILE IF NULL

    [Fact]
    public async Task ShouldCreateProfile_WhenProfileNotExists()
    {
        var handler = CreateHandler();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "test",
            Email = "test@mail.com"
        };

        _userRepo.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _profileRepo.Setup(x => x.GetByUserIdAsync(user.Id)).ReturnsAsync((Profile?)null);

        var command = new UpdateUserProfileCommand
        {
            UserId = user.Id,
            Bio = "Hello"
        };

        await handler.Handle(command, CancellationToken.None);

        _profileRepo.Verify(x => x.CreateAsync(It.IsAny<Profile>()), Times.Once);
    }

    //UPDATE PROFILE FIELDS
    [Fact]
    public async Task ShouldUpdateProfileFields_WhenProvided()
    {
        var handler = CreateHandler();

        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Username = "test",
            Email = "test@mail.com"
        };

        var profile = new Profile
        {
            Id = Guid.NewGuid(),
            UserId = userId
        };

        _userRepo.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _profileRepo.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(profile);

        var command = new UpdateUserProfileCommand
        {
            UserId = userId,
            Bio = "Bio",
            Phone = "123",
            Location = "HCM"
        };

        await handler.Handle(command, CancellationToken.None);

        profile.Bio.Should().Be("Bio");
        profile.Phone.Should().Be("123");
        profile.Location.Should().Be("HCM");

        _profileRepo.Verify(x => x.UpdateAsync(profile), Times.Once);
    }


    // UPDATE USER NAME
    [Fact]
    public async Task ShouldUpdateUserName_WhenProvided()
    {
        var handler = CreateHandler();

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Old",
            LastName = "Name"
        };

        _userRepo.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _profileRepo.Setup(x => x.GetByUserIdAsync(user.Id))
            .ReturnsAsync(new Profile { UserId = user.Id });

        var command = new UpdateUserProfileCommand
        {
            UserId = user.Id,
            FirstName = "New",
            LastName = "Name"
        };

        await handler.Handle(command, CancellationToken.None);

        _userRepo.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

   
    //SAVE CHANGES WHEN UPDATED
    [Fact]
    public async Task ShouldSaveChanges_WhenAnyUpdateOccurs()
    {
        var handler = CreateHandler();

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Old",
            LastName = "Name"
        };

        _userRepo.Setup(x => x.GetByIdAsync(user.Id))
            .ReturnsAsync(user);

        _profileRepo.Setup(x => x.GetByUserIdAsync(user.Id))
            .ReturnsAsync(new Profile { UserId = user.Id });

        var command = new UpdateUserProfileCommand
        {
            UserId = user.Id,
            FirstName = "New" // 👈 đủ để trigger UpdateName thật
        };

        await handler.Handle(command, CancellationToken.None);

        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }


    //PUBLISH EVENT

    [Fact]
    public async Task ShouldPublishEvent_WhenSuccess()
    {
        var handler = CreateHandler();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "test",
            Email = "test@mail.com",
            IsActive = true
        };

        var profile = new Profile { UserId = user.Id };

        _userRepo.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _profileRepo.Setup(x => x.GetByUserIdAsync(user.Id)).ReturnsAsync(profile);

        var command = new UpdateUserProfileCommand
        {
            UserId = user.Id,
            Bio = "test"
        };

        await handler.Handle(command, CancellationToken.None);

        _outbox.Verify(x => x.EnqueueAsync(
            RoutingKeys.Auth.UserProfileUpdated,
            It.IsAny<UserProfileUpdatedEvent>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }


    //NO UPDATE → NO SAVE
    [Fact]
    public async Task ShouldNotSave_WhenNoChanges()
    {
        var handler = CreateHandler();

        var user = new User { Id = Guid.NewGuid() };

        _userRepo.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _profileRepo.Setup(x => x.GetByUserIdAsync(user.Id))
            .ReturnsAsync(new Profile { UserId = user.Id });

        var command = new UpdateUserProfileCommand
        {
            UserId = user.Id
        };

        await handler.Handle(command, CancellationToken.None);

        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    //Update profile fail
    [Fact]
    public async Task ShouldThrow_WhenProfileUpdateFails()
    {
        var handler = CreateHandler();

        var userId = Guid.NewGuid();

        var user = new User { Id = userId };
        var profile = new Profile { UserId = userId };

        _userRepo.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _profileRepo.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(profile);

        _profileRepo.Setup(x => x.UpdateAsync(It.IsAny<Profile>()))
            .ThrowsAsync(new Exception("DB error"));

        var command = new UpdateUserProfileCommand
        {
            UserId = userId,
            Bio = "test"
        };

        await Assert.ThrowsAsync<Exception>(() =>
            handler.Handle(command, CancellationToken.None));
    }


    //Outbox publish fail
    [Fact]
    public async Task ShouldThrow_WhenOutboxFails()
    {
        var handler = CreateHandler();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "test",
            Email = "test@mail.com"
        };

        var profile = new Profile { UserId = user.Id };

        _userRepo.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _profileRepo.Setup(x => x.GetByUserIdAsync(user.Id)).ReturnsAsync(profile);

        _outbox.Setup(x => x.EnqueueAsync(
            It.IsAny<string>(),
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Outbox error"));

        var command = new UpdateUserProfileCommand
        {
            UserId = user.Id,
            Bio = "test"
        };

        await Assert.ThrowsAsync<Exception>(() =>
            handler.Handle(command, CancellationToken.None));
    }
}