using Xunit;
using Moq;
using FluentAssertions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AuthService.Application.Features.Auth.Handlers;
using AuthService.Application.Features.Auth.Commands;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Application.Results;
using Shared.Contracts;
using Shared.Contracts.Events.Activity;


namespace AuthService.Tests.Features.Auth;
public class LoginHandlerTests
{
    private readonly Mock<IAuthRepository> _repo = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepo = new();
    private readonly Mock<IOutboxRepository> _outbox = new();
    private readonly Mock<ILogger<LoginHandler>> _logger = new();
    private readonly Mock<IDateTimeProvider> _date = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private LoginHandler CreateHandler()
    {
        return new LoginHandler(
            _repo.Object,
            _jwt.Object,
            _refreshTokenRepo.Object,
            _outbox.Object,
            _logger.Object,
            _date.Object,
            _uow.Object
        );
    }

    // =========================
    // ✅ SUCCESS
    // =========================
    [Fact]
    public async Task Login_ShouldSuccess_WhenValidCredentials()
    {
        var handler = CreateHandler();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@mail.com",
            Username = "test",
            Password = BCrypt.Net.BCrypt.HashPassword("Valid@123"),
            IsActive = true
        };

        _repo.Setup(x => x.GetByUsernameOrEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        _jwt.Setup(x => x.GenerateTokenPair(user))
            .Returns(("access", "refresh"));

        var command = new LoginCommand
        {
            Identifier = "test@mail.com",
            Password = "Valid@123"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _outbox.Verify(x => x.EnqueueAsync(
            RoutingKeys.Auth.LoginSuccessful,
            It.IsAny<LoginSuccessfulEvent>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================
    // ❌ USER NOT FOUND
    // =========================
    [Fact]
    public async Task Login_ShouldFail_WhenUserNotFound()
    {
        var handler = CreateHandler();

        _repo.Setup(x => x.GetByUsernameOrEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var command = new LoginCommand
        {
            Identifier = "notfound@mail.com",
            Password = "123"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();

        _outbox.Verify(x => x.EnqueueAsync(
            RoutingKeys.Auth.LoginFailed,
            It.IsAny<LoginFailedEvent>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================
    // ❌ WRONG PASSWORD
    // =========================
    [Fact]
    public async Task Login_ShouldFail_WhenWrongPassword()
    {
        var handler = CreateHandler();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@mail.com",
            Password = BCrypt.Net.BCrypt.HashPassword("correct"),
            FailedLoginAttempts = 0
        };

        _repo.Setup(x => x.GetByUsernameOrEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        var command = new LoginCommand
        {
            Identifier = "test@mail.com",
            Password = "wrong"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        user.FailedLoginAttempts.Should().Be(1);
    }

    // =========================
    // ❌ LOCK ACCOUNT AFTER 5 FAILS
    // =========================
    [Fact]
    public async Task Login_ShouldLockAccount_WhenExceededMaxAttempts()
    {
        var handler = CreateHandler();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@mail.com",
            Password = BCrypt.Net.BCrypt.HashPassword("correct"),
            FailedLoginAttempts = 4
        };

        _repo.Setup(x => x.GetByUsernameOrEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        _date.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var command = new LoginCommand
        {
            Identifier = "test@mail.com",
            Password = "wrong"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        user.IsLocked.Should().BeTrue();

        _outbox.Verify(x => x.EnqueueAsync(
            RoutingKeys.Auth.LoginFailed,
            It.IsAny<LoginFailedEvent>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================
    // ❌ ACCOUNT LOCKED
    // =========================
    [Fact]
    public async Task Login_ShouldFail_WhenAccountAlreadyLocked()
    {
        var handler = CreateHandler();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@mail.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Valid@123"),
            IsActive = true,
            IsLocked = true
        };

        _repo.Setup(x => x.GetByUsernameOrEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        var command = new LoginCommand
        {
            Identifier = "test@mail.com",
            Password = "Valid@123"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    // =========================
    // ❌ EMAIL NOT VERIFIED
    // =========================
    [Fact]
    public async Task Login_ShouldFail_WhenEmailNotVerified()
    {
        var handler = CreateHandler();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@mail.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Valid@123"),
            IsActive = false,
            EmailVerifiedAt = null
        };

        _repo.Setup(x => x.GetByUsernameOrEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        var command = new LoginCommand
        {
            Identifier = "test@mail.com",
            Password = "Valid@123"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    // =========================
    // ❌ ACCOUNT BANNED
    // =========================
    [Fact]
    public async Task Login_ShouldFail_WhenAccountDeactivated()
    {
        var handler = CreateHandler();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@mail.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Valid@123"),
            IsActive = false,
            EmailVerifiedAt = DateTime.UtcNow
        };

        _repo.Setup(x => x.GetByUsernameOrEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        var command = new LoginCommand
        {
            Identifier = "test@mail.com",
            Password = "Valid@123"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    // =========================
    // ❌ PENDING DELETION
    // =========================
    [Fact]
    public async Task Login_ShouldFail_WhenPendingDeletion()
    {
        var handler = CreateHandler();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@mail.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Valid@123"),
            IsActive = false,
            DeletionScheduledAt = DateTime.UtcNow.AddDays(1)
        };

        _date.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        _repo.Setup(x => x.GetByUsernameOrEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        var command = new LoginCommand
        {
            Identifier = "test@mail.com",
            Password = "Valid@123"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    // =========================
    // ✅ REMEMBER ME = TRUE
    // =========================
    [Fact]
    public async Task Login_ShouldSetRefreshToken_30Days_WhenRememberMe()
    {
        var handler = CreateHandler();

        var now = DateTime.UtcNow;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@mail.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Valid@123"),
            IsActive = true
        };

        _date.Setup(x => x.UtcNow).Returns(now);

        _repo.Setup(x => x.GetByUsernameOrEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        _jwt.Setup(x => x.GenerateTokenPair(user))
            .Returns(("access", "refresh"));

        var command = new LoginCommand
        {
            Identifier = "test@mail.com",
            Password = "Valid@123",
            RememberMe = true
        };

        await handler.Handle(command, CancellationToken.None);

        _refreshTokenRepo.Verify(x => x.AddAsync(
            It.Is<RefreshToken>(rt =>
                rt.ExpiresAt == now.AddDays(30)
            )), Times.Once);
    }

    // =========================
    // ✅ REMEMBER ME = FALSE
    // =========================
    [Fact]
    public async Task Login_ShouldSetRefreshToken_7Days_WhenNotRememberMe()
    {
        var handler = CreateHandler();

        var now = DateTime.UtcNow;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@mail.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Valid@123"),
            IsActive = true
        };

        _date.Setup(x => x.UtcNow).Returns(now);

        _repo.Setup(x => x.GetByUsernameOrEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        _jwt.Setup(x => x.GenerateTokenPair(user))
            .Returns(("access", "refresh"));

        var command = new LoginCommand
        {
            Identifier = "test@mail.com",
            Password = "Valid@123",
            RememberMe = false
        };

        await handler.Handle(command, CancellationToken.None);

        _refreshTokenRepo.Verify(x => x.AddAsync(
            It.Is<RefreshToken>(rt =>
                rt.ExpiresAt == now.AddDays(7)
            )), Times.Once);
    }
}