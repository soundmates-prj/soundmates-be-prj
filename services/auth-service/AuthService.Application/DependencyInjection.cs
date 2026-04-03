using System.Reflection;
using AuthService.Application.Abstractions;
using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Abstractions.Messaging.Dispatcher;
using AuthService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AuthService.Application.Results;
using AuthService.Application.Features.Auth.Commands;
using AuthService.Application.Features.Auth.Handlers;
using AuthService.Application.Features.Common;
using AuthService.Application.Features.Role.Commands;
using AuthService.Application.Features.Role.Handlers;
using AuthService.Application.Features.SpotifyAuth.Commands;
using AuthService.Application.Features.SpotifyAuth.Handlers;
using AuthService.Application.Features.SpotifyItems.Commands;
using AuthService.Application.Features.SpotifyItems.Handlers;
using AuthService.Application.Features.Users.Commands;
using AuthService.Application.Features.Users.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthApplication(this IServiceCollection services)
    {
        // Dispatchers
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();

        // Handlers (explicit registrations)
        // User handlers
        services.AddScoped<ICommandHandler<CreateUserCommand, Guid>, CreateUserHandler>();
        services.AddScoped<ICommandHandler<UpdateUserCommand, bool>, UpdateUserHandler>();
        services.AddScoped<ICommandHandler<DeleteUserCommand, bool>, DeleteUserHandler>();
        services.AddScoped<ICommandHandler<BanUserCommand, bool>, BanUserHandler>();
        services.AddScoped<ICommandHandler<UnbanUserCommand, bool>, UnbanUserHandler>();
        services.AddScoped<ICommandHandler<DeactivateUserCommand, bool>, DeactivateUserHandler>();
        services.AddScoped<ICommandHandler<ActivateUserCommand, bool>, ActivateUserHandler>();
        services.AddScoped<ICommandHandler<UpdateAccountStatusCommand, bool>, UpdateAccountStatusHandler>();
        services.AddScoped<ICommandHandler<VerifyUserEmailCommand, bool>, VerifyUserEmailHandler>();
        services.AddScoped<ICommandHandler<CreateUserFavouriteCommand, Guid>, CreateUserFavouriteHandler>();
        services.AddScoped<ICommandHandler<UpdateUserFavouriteCommand, bool>, UpdateUserFavouriteHandler>();
        services.AddScoped<ICommandHandler<DeleteUserFavouriteCommand, bool>, DeleteUserFavouriteHandler>();
        services.AddScoped<ICommandHandler<CreateSpotifyItemCommand, Guid>, CreateSpotifyItemHandler>();
        services.AddScoped<ICommandHandler<DeleteSpotifyItemCommand, bool>, DeleteSpotifyItemHandler>();
        services.AddScoped<ICommandHandler<GetSpotifyLoginUrlCommand, string>, GetSpotifyLoginUrlHandler>();
        services.AddScoped<ICommandHandler<ConnectSpotifyCommand, bool>, ConnectSpotifyHandler>();
        services.AddScoped<ICommandHandler<GetSpotifyProfileCommand, SpotifyUserProfile>, GetSpotifyProfileHandler>();
        
        // Role handlers
        services.AddScoped<ICommandHandler<CreateRoleCommand, Guid>, CreateRoleHandler>();
        services.AddScoped<ICommandHandler<UpdateRoleCommand, bool>, UpdateRoleHandler>();
        services.AddScoped<ICommandHandler<DeleteRoleCommand, bool>, DeleteRoleHandler>();
        
        // Auth handlers
        services.AddScoped<ICommandHandler<LoginCommand, AuthResult>, LoginHandler>();
        services.AddScoped<ICommandHandler<RegisterCommand, AuthResult>, RegisterHandler>();
        services.AddScoped<ICommandHandler<GoogleLoginCommand, AuthResult>, GoogleLoginHandler>();
        services.AddScoped<ICommandHandler<RefreshTokenCommand, AuthResult>, RefreshTokenHandler>();
        services.AddScoped<ICommandHandler<VerifyEmailCommand, AuthResult>, VerifyEmailHandler>();
        services.AddScoped<ICommandHandler<ForgetPasswordCommand, bool>, ForgetPasswordHandler>();
        services.AddScoped<ICommandHandler<ResetPasswordCommand, bool>, ResetPasswordHandler>();
        services.AddScoped<ICommandHandler<ChangePasswordCommand, bool>, ChangePasswordHandler>();
        services.AddScoped<ICommandHandler<ResendOtpCommand, bool>, ResendOtpHandler>();
        services.AddScoped<ICommandHandler<DeactivateAccountCommand, bool>, DeactivateAccountHandler>();
        services.AddScoped<ICommandHandler<RequestAccountDeletionCommand, bool>, RequestAccountDeletionHandler>();
        services.AddScoped<ICommandHandler<CancelAccountDeletionCommand, bool>, CancelAccountDeletionHandler>();

        // Profile handler
        services.AddScoped<ICommandHandler<UpdateUserProfileCommand, UserProfileResult>, UpdateUserProfileHandler>();
        
        return services;
    }
}
