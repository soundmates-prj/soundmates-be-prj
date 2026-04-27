using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Mappings;
using AuthService.Application.Results;
using AuthService.Application.Features.Auth.Commands;
using AuthService.Application.Features.Common;
using AuthService.Domain.Enums;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;
using AuthService.Domain.Rules;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts;
using Shared.Contracts.Events.Auth;
using Shared.Contracts.Events.Activity;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Features.Auth.Handlers;

public sealed class RegisterHandler : ICommandHandler<RegisterCommand, AuthResult>
{
    private readonly IAuthRepository _repo;
    private readonly IOutboxRepository _outbox;
    private readonly ILogger<RegisterHandler> _logger;
    private readonly IOtpService _otpService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public RegisterHandler(
        IAuthRepository repo,
        IOutboxRepository outbox,
        ILogger<RegisterHandler> logger,
        IOtpService otpService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        IServiceScopeFactory serviceScopeFactory)
    {
        _repo = repo;
        _outbox = outbox;
        _logger = logger;
        _otpService = otpService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<Result<AuthResult>> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // Validate password using Domain Rule
            try
            {
                PasswordRule.Validate(command.Password);
            }
            catch (UserValidationException ex)
            {
                // Publish registration failed event (Activity — non-critical audit trail)
                var failEvt = new RegistrationFailedEvent
                {
                    Username = command.Username,
                    Email = command.Email,
                    Reason = ex.Message,
                    ErrorCode = ex.ErrorCode ?? string.Empty
                };
                await _outbox.EnqueueAsync(
                    RoutingKeys.Auth.RegistrationFailed, failEvt, cancellationToken);

                return Result<AuthResult>.Failure(ex.Message, ex.StatusCode);
            }

            // Execute in an explicit transaction for atomicity (User + Outbox)
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var user = await _repo.RegisterAsync(
                    command.Username, command.Email, command.Password,
                    command.FirstName, command.LastName);

                // Publish typed UserCreatedEvent to Outbox (Auth — domain state change)
                var evt = new UserCreatedEvent
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty,
                    RoleId = user.RoleId ?? Guid.Empty,
                    RoleName = user.Role?.Name ?? "MEMBER",
                    IsActive = user.IsActive,
                    AccountStatus = (int)Application.Enums.AccountStatusEnum.Active,
                    CreatedAt = user.CreatedAt ?? DateTime.UtcNow
                };
                await _outbox.EnqueueAsync(RoutingKeys.Auth.UserCreated, evt, cancellationToken);

                // Commit both User and OutboxMessage in one transaction
                await _unitOfWork.CommitAsync(cancellationToken);

                // Run email OTP in Background to avoid Gateway Timeouts
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var backgroundOtpService = scope.ServiceProvider.GetRequiredService<IOtpService>();
                        await backgroundOtpService.GenerateAndSendOtpAsync(
                            user.Email, user.Username, user.FirstName,
                            OtpPurpose.EmailVerification, CancellationToken.None);
                        _logger.LogInformation("Verification OTP sent in background to {Email}", user.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Background OTP send failed for {Email}", user.Email);
                    }
                }, CancellationToken.None);

                return Result<AuthResult>.Success(
                    user.ToAuthResult(null, null),
                    "Registration successful. Please check your email to verify your account shortly.");
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Application.Exceptions.AuthException ex)
        {
            // Publish registration failed event (enum ErrorCode → int → string)
            try
            {
                var failEvt = new RegistrationFailedEvent
                {
                    Username = command.Username,
                    Email = command.Email,
                    Reason = ex.Message,
                    ErrorCode = ((int)ex.ErrorCode).ToString()
                };
                await _outbox.EnqueueAsync(
                    RoutingKeys.Auth.RegistrationFailed, failEvt, cancellationToken);
            }
            catch { }

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed unexpected: {Message}", ex.Message);
            throw new InvalidOperationException($"Registration failed: {ex.Message}", ex);
        }
    }
}
