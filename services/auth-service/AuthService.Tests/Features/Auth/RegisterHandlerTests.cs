using AuthService.Application.Enums;
using AuthService.Application.Exceptions;
using AuthService.Application.Features.Auth.Commands;
using AuthService.Application.Features.Auth.Handlers;
using AuthService.Application.Features.Common;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Contracts;

namespace AuthService.Tests.Features.Auth
{
    public class RegisterHandlerTests
    {
        private readonly Mock<IAuthRepository> _repo = new();
        private readonly Mock<IOutboxRepository> _outbox = new();
        private readonly Mock<ILogger<RegisterHandler>> _logger = new();
        private readonly Mock<IOtpService> _otp = new();
        private readonly Mock<IDateTimeProvider> _date = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IServiceScopeFactory> _scopeFactory = new();

        private RegisterHandler CreateHandler()
        {
            return new RegisterHandler(
                _repo.Object,
                _outbox.Object,
                _logger.Object,
                _otp.Object,
                _date.Object,
                _uow.Object,
                _scopeFactory.Object
            );
        }

        //Password invalid
        [Fact]
        public async Task Register_ShouldFail_WhenPasswordInvalid()
        {
            var handler = CreateHandler();

            var command = new RegisterCommand
            {
                FirstName = "Minh",
                LastName = "Nguyen",
                Username = "test",
                Email = "test@mail.com",
                Password = "123" // invalid
            };

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            _outbox.Verify(x => x.EnqueueAsync(
                RoutingKeys.Auth.RegistrationFailed,
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        //Input valid
        [Fact]
        public async Task Register_ShouldSuccess_WhenValidInput()
        {
            var handler = CreateHandler();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "test",
                Email = "test@mail.com",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _repo.Setup(x => x.RegisterAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).ReturnsAsync(user);

            var command = new RegisterCommand
            {
                FirstName = "Minh",
                LastName = "Nguyen",
                Username = "test",
                Email = "test@mail.com",
                Password = "Valid@12345678"
            };

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();

            _repo.Verify(x => x.RegisterAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
                Times.Once);

            _outbox.Verify(x => x.EnqueueAsync(
                RoutingKeys.Auth.UserCreated,
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
                Times.Once);

            _uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        //DB lỗi
        [Fact]
        public async Task Register_ShouldRollback_WhenRepoFails()
        {
            var handler = CreateHandler();

            _repo.Setup(x => x.RegisterAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).ThrowsAsync(new Exception("DB error"));

            var command = new RegisterCommand
            {
                FirstName = "Minh",
                LastName = "Nguyen",
                Username = "test",
                Email = "test@mail.com",
                Password = "Valid@12345678"
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));

            _uow.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        //Test Username duplicate
        [Fact]
        public async Task Register_ShouldFail_WhenUsernameAlreadyExists()
        {
            // Arrange
            var handler = CreateHandler();

            _repo.Setup(x => x.RegisterAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).ThrowsAsync(new AuthException(
                AuthErrorCode.UserAlreadyExists,
                "Username 'test' is already taken"
            ));

            var command = new RegisterCommand
            {
                FirstName = "Minh",
                LastName = "Nguyen",
                Username = "test",
                Email = "new@mail.com",
                Password = "Valid@1234567845678"
            };

            // Act
            var ex = await Assert.ThrowsAsync<AuthException>(() =>
                handler.Handle(command, CancellationToken.None));

            // Assert
            ex.Message.Should().Contain("Username");

            _outbox.Verify(x => x.EnqueueAsync(
                RoutingKeys.Auth.RegistrationFailed,
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        //Test Email duplicate
        [Fact]
        public async Task Register_ShouldFail_WhenEmailAlreadyExists()
        {
            // Arrange
            var handler = CreateHandler();

            _repo.Setup(x => x.RegisterAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).ThrowsAsync(new AuthException(
                AuthErrorCode.UserAlreadyExists,
                "Email 'test@mail.com' is already registered"
            ));

            var command = new RegisterCommand
            {
                FirstName = "Minh",
                LastName = "Nguyen",
                Username = "newuser",
                Email = "test@mail.com",
                Password = "Valid@12345678"
            };

            // Act
            var ex = await Assert.ThrowsAsync<AuthException>(() =>
                handler.Handle(command, CancellationToken.None));

            // Assert
            ex.Message.Should().Contain("Email");

            _outbox.Verify(x => x.EnqueueAsync(
                RoutingKeys.Auth.RegistrationFailed,
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        //AuthException
        [Fact]
        public async Task Register_ShouldPublishFailEvent_WhenAuthException()
        {
            var handler = CreateHandler();

            _repo.Setup(x => x.RegisterAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).ThrowsAsync(new AuthException(AuthErrorCode.InvalidCredentials, "Invalid credentials"));

            var command = new RegisterCommand
            {
                FirstName = "Minh",
                LastName = "Nguyen",
                Username = "test",
                Email = "test@mail.com",
                Password = "Valid@12345678"
            };

            await Assert.ThrowsAsync<AuthException>(() =>
                handler.Handle(command, CancellationToken.None));

            _outbox.Verify(x => x.EnqueueAsync(
                RoutingKeys.Auth.RegistrationFailed,
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }
    }
}
