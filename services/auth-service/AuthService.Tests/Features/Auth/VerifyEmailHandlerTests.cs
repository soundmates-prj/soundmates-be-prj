using AuthService.Application.Features.Auth.Commands;
using AuthService.Application.Features.Auth.Handlers;
using AuthService.Application.Results;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Shared.Contracts;
using Shared.Contracts.Events.Auth;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static System.Net.WebRequestMethods;


namespace AuthService.Tests.Features.Auth;
public class VerifyEmailHandlerTests
{
    private readonly Mock<IAuthRepository> _repo = new();
    private readonly Mock<IOtpRepository> _otpRepo = new();
    private readonly Mock<IOutboxRepository> _outbox = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IDateTimeProvider> _date = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepo = new();

    private VerifyEmailHandler CreateHandler()
    {
        return new VerifyEmailHandler(
            _repo.Object,
            _otpRepo.Object,
            _outbox.Object,
            _uow.Object,
            _date.Object,
            _jwt.Object,
            _refreshTokenRepo.Object
        );
    }

   
    // OtpCode INVALID

    [Fact]
    public async Task VerifyEmail_ShouldFail_WhenOtpInvalid()
    {
        var handler = CreateHandler();

        _otpRepo.Setup(x => x.GetByEmailAndCodeAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<OtpPurpose>()))
            .ReturnsAsync((OtpCode?)null);

        var command = new VerifyEmailCommand
        {
            Email = "test@mail.com",
            OtpCode = "wrong"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid or expired OTP code");

        _uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    //USER NULL (VERIFY FAIL)

    [Fact]
    public async Task VerifyEmail_ShouldFail_WhenUserNull()
    {
        var handler = CreateHandler();

        var OtpCode = new OtpCode
        {
            Email = "test@mail.com",
            Code = "123456"
        };

        _otpRepo.Setup(x => x.GetByEmailAndCodeAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<OtpPurpose>()))
            .ReturnsAsync(OtpCode);

        _repo.Setup(x => x.VerifyEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var command = new VerifyEmailCommand
        {
            Email = "test@mail.com",
            OtpCode = "123456"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();

        _uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

  
    //  SUCCESS
    [Fact]
    public async Task VerifyEmail_ShouldSuccess_WhenValidOtp()
    {
        var handler = CreateHandler();

        var now = DateTime.UtcNow;

        var OtpCode = new OtpCode
        {
            Email = "test@mail.com",
            Code = "123456",
            IsUsed = false
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@mail.com",
            Username = "test"
        };

        _date.Setup(x => x.UtcNow).Returns(now);

        _otpRepo.Setup(x => x.GetByEmailAndCodeAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<OtpPurpose>()))
            .ReturnsAsync(OtpCode);

        _repo.Setup(x => x.VerifyEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(user);

        _jwt.Setup(x => x.GenerateTokenPair(user))
            .Returns(("access", "refresh"));

        var command = new VerifyEmailCommand
        {
            Email = "test@mail.com",
            OtpCode = "123456"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // OtpCode updated
        OtpCode.IsUsed.Should().BeTrue();
        OtpCode.UsedAt.Should().Be(now);

        _otpRepo.Verify(x => x.UpdateAsync(It.IsAny<OtpCode>()), Times.Once);

        // Refresh token saved
        _refreshTokenRepo.Verify(x => x.AddAsync(
            It.Is<RefreshToken>(rt =>
                rt.UserId == user.Id &&
                rt.Token == "refresh"
            )), Times.Once);

        // Event published
        _outbox.Verify(x => x.EnqueueAsync(
            RoutingKeys.Auth.UserUpdated,
            It.IsAny<UserUpdatedEvent>(),
            It.IsAny<CancellationToken>()),
            Times.Once);

        // Commit called
        _uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    
    //OtpCode UPDATE FAIL
    [Fact]
    public async Task VerifyEmail_ShouldRollback_WhenOtpUpdateFails()
    {
        var handler = CreateHandler();

        var OtpCode = new OtpCode { Email = "test@mail.com", Code = "123" };
        var user = new User { Id = Guid.NewGuid() };

        _otpRepo.Setup(x => x.GetByEmailAndCodeAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<OtpPurpose>()))
            .ReturnsAsync(OtpCode);

        _repo.Setup(x => x.VerifyEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(user);

        _otpRepo.Setup(x => x.UpdateAsync(It.IsAny<OtpCode>()))
            .ThrowsAsync(new Exception("DB error"));

        await Assert.ThrowsAsync<Exception>(() =>
            handler.Handle(new VerifyEmailCommand
            {
                Email = "test@mail.com",
                OtpCode = "123"
            }, CancellationToken.None));

        _uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

   
    // REFRESH TOKEN FAIL
    [Fact]
    public async Task VerifyEmail_ShouldRollback_WhenRefreshTokenFails()
    {
        var handler = CreateHandler();

        var OtpCode = new OtpCode { Email = "test@mail.com", Code = "123" };
        var user = new User { Id = Guid.NewGuid() };

        _otpRepo.Setup(x => x.GetByEmailAndCodeAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<OtpPurpose>()))
            .ReturnsAsync(OtpCode);

        _repo.Setup(x => x.VerifyEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(user);

        _jwt.Setup(x => x.GenerateTokenPair(user))
            .Returns(("access", "refresh"));

        _refreshTokenRepo.Setup(x => x.AddAsync(It.IsAny<RefreshToken>()))
            .ThrowsAsync(new Exception("DB error"));

        await Assert.ThrowsAsync<Exception>(() =>
            handler.Handle(new VerifyEmailCommand
            {
                Email = "test@mail.com",
                OtpCode = "123"
            }, CancellationToken.None));

        _uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }


    //OUTBOX FAIL
 
    [Fact]
    public async Task VerifyEmail_ShouldRollback_WhenOutboxFails()
    {
        var handler = CreateHandler();

        var OtpCode = new OtpCode { Email = "test@mail.com", Code = "123" };
        var user = new User { Id = Guid.NewGuid() };

        _otpRepo.Setup(x => x.GetByEmailAndCodeAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<OtpPurpose>()))
            .ReturnsAsync(OtpCode);

        _repo.Setup(x => x.VerifyEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(user);

        _jwt.Setup(x => x.GenerateTokenPair(user))
            .Returns(("access", "refresh"));

        _outbox.Setup(x => x.EnqueueAsync(
            It.IsAny<string>(),
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Outbox error"));

        await Assert.ThrowsAsync<Exception>(() =>
            handler.Handle(new VerifyEmailCommand
            {
                Email = "test@mail.com",
                OtpCode = "123"
            }, CancellationToken.None));

        _uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}