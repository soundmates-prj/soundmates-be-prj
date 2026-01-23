using System.Reflection;
using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Abstractions.Messaging.Dispatcher;
using AuthService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AuthService.Application.DTOs;
using AuthService.Application.Services.Auth.Commands;
using AuthService.Application.Services.Auth.Handlers;
using AuthService.Application.Services.Common;
using AuthService.Application.Services.Role.Commands;
using AuthService.Application.Services.Role.Handlers;
using AuthService.Application.Services.Users.Commands;
using AuthService.Application.Services.Users.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthApplication(this IServiceCollection services)
    {
        // Dispatchers
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();

        // Handlers (explicit registrations)
        services.AddScoped<ICommandHandler<CreateUserCommand, Guid>, CreateUserHandler>();
        services.AddScoped<ICommandHandler<UpdateUserCommand, bool>, UpdateUserHandler>();
        services.AddScoped<ICommandHandler<DeleteUserCommand, bool>, DeleteUserHandler>();
        services.AddScoped<ICommandHandler<BanUserCommand, bool>, BanUserHandler>();
        services.AddScoped<ICommandHandler<UnbanUserCommand, bool>, UnbanUserHandler>();
        services.AddScoped<ICommandHandler<DeactivateUserCommand, bool>, DeactivateUserHandler>();
        services.AddScoped<ICommandHandler<CreateRoleCommand, Guid>, CreateRoleHandler>();
        services.AddScoped<ICommandHandler<UpdateRoleCommand, bool>, UpdateRoleHandler>();
        services.AddScoped<ICommandHandler<DeleteRoleCommand, bool>, DeleteRoleHandler>();
        services.AddScoped<ICommandHandler<LoginCommand, UserDto>, LoginHandler>();
        services.AddScoped<ICommandHandler<RegisterCommand, UserDto>, RegisterHandler>();
        services.AddScoped<ICommandHandler<GoogleLoginCommand, UserDto>, GoogleLoginHandler>();
        services.AddScoped<ICommandHandler<RefreshTokenCommand, UserDto>, RefreshTokenHandler>();
        services.AddScoped<ICommandHandler<ForgetPasswordRequestCommand, bool>, ForgetPasswordRequestHandler>();
        services.AddScoped<ICommandHandler<ResetPasswordCommand, bool>, ResetPasswordHandler>();
        services.AddScoped<ICommandHandler<ChangePasswordCommand, bool>, ChangePasswordHandler>();
        services.AddScoped<ICommandHandler<UpdateProfileCommand, UserDto>, UpdateProfileHandler>();
        services.AddScoped<ICommandHandler<UpdateProfileOptionsCommand, UserDto>, UpdateProfileOptionsHandler>();
        services.AddScoped<ICommandHandler<VerifyEmailCommand, UserDto>, VerifyEmailHandler>();
        services.AddScoped<ICommandHandler<ResendOtpCommand, bool>, ResendOtpHandler>();
        
        // Application services (only OtpService remains in Common - legitimate application service)
        services.AddScoped<IOtpService, OtpService>();

        return services;
    }
}