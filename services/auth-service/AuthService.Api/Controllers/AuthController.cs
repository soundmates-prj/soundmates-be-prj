using AuthService.Api.Models.Requests;
using AuthService.Api.Models.Requests.User;
using AuthService.Api.Models.Responses;
using AuthService.Api.Extensions;
using AuthService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AuthService.Application.Enums;
using AuthService.Application.Exceptions;
using AuthService.Application.Results;
using AuthService.Application.Features.Auth.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers
{
    /// <summary>
    /// Authentication and account security endpoints.
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        // khoi tao dispatcher
        private readonly ICommandDispatcher _commands;

        public AuthController(ICommandDispatcher commands)
        {
            _commands = commands;
        }

        /// <summary>
        /// Authenticates with username/email and password.
        /// </summary>
        /// <remarks>
        /// Returns access token and refresh token when credentials are valid.
        /// </remarks>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        {
            // check model state validations
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<AuthResult>.FailureResponse("Invalid input, please try again", 400));
            
            // Get IP address and User-Agent for security logging
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() 
                ?? HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                ?? "Unknown";
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            // create Login Command from Request
            var cmd = new LoginCommand
            {
                Identifier = request.EmailOrUsername,
                Password = request.Password,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            // Send Login Command to Handler - returns Result<AuthResult>
            var result = await _commands.Send<LoginCommand, AuthResult>(cmd, ct);

            // Check result and return appropriate HTTP response
            if (!result.IsSuccess)
            {
                return result.ErrorCode switch
                {
                    401 => Unauthorized(ApiResponse<AuthResult>.FailureResponse(result.ErrorMessage ?? "Unauthorized", 401)),
                    403 => StatusCode(403, ApiResponse<AuthResult>.FailureResponse(result.ErrorMessage ?? "Forbidden", 403)),
                    _ => BadRequest(ApiResponse<AuthResult>.FailureResponse(result.ErrorMessage ?? "Login failed", result.ErrorCode ?? 400))
                };
            }

            return Ok(ApiResponse<AuthResult>.SuccessResponse(result.Data!, result.ErrorMessage ?? "Login successful"));
        }

        /// <summary>
        /// Registers a new user account.
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
        {
            // check model state validations
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<AuthResult>.FailureResponse("Invalid input", 400));
            
            // Mapping from Request into Command
            var cmd = new RegisterCommand
            {
                Username = request.Username,
                Email = request.Email,
                Password = request.Password,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            // Send Register Command to Handler - returns Result<AuthResult>
            var result = await _commands.Send<RegisterCommand, AuthResult>(cmd, ct);

            if (!result.IsSuccess)
            {
                return result.ErrorCode switch
                {
                    409 => Conflict(ApiResponse<AuthResult>.FailureResponse(result.ErrorMessage ?? "User already exists", 409)),
                    _ => BadRequest(ApiResponse<AuthResult>.FailureResponse(result.ErrorMessage ?? "Registration failed", result.ErrorCode ?? 400))
                };
            }

            return Ok(ApiResponse<AuthResult>.SuccessResponse(result.Data!, result.ErrorMessage ?? "Registration successful"));
        }

        /// <summary>
        /// Authenticates a user using Google ID token.
        /// </summary>
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<AuthResult>.FailureResponse("Invalid input", 400));

            var cmd = new GoogleLoginCommand
            {
                IdToken = request.IdToken
            };

            var result = await _commands.Send<GoogleLoginCommand, AuthResult>(cmd, ct);

            if (!result.IsSuccess)
            {
                return result.ErrorCode switch
                {
                    401 => Unauthorized(ApiResponse<AuthResult>.FailureResponse(result.ErrorMessage ?? "Unauthorized", 401)),
                    _ => BadRequest(ApiResponse<AuthResult>.FailureResponse(result.ErrorMessage ?? "Login failed", result.ErrorCode ?? 400))
                };  
            }

            return Ok(ApiResponse<AuthResult>.SuccessResponse(result.Data!, result.ErrorMessage ?? "Google login successful"));
        }

        /// <summary>
        /// Issues a new access token using a valid refresh token.
        /// </summary>
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<AuthResult>.FailureResponse("Invalid input", 400));

            var cmd = new RefreshTokenCommand
            {
                RefreshToken = request.RefreshToken
            };

            var result = await _commands.Send<RefreshTokenCommand, AuthResult>(cmd, ct);

            if (!result.IsSuccess)
            {
                return Unauthorized(ApiResponse<AuthResult>.FailureResponse(result.ErrorMessage ?? "Invalid or expired refresh token", 401));
            }

            return Ok(ApiResponse<AuthResult>.SuccessResponse(result.Data!, result.ErrorMessage ?? "Token refreshed successfully"));
        }

        /// <summary>
        /// Verifies user email with OTP code.
        /// </summary>
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<AuthResult>.FailureResponse("Invalid input", 400));

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.OtpCode))
            {
                return BadRequest(ApiResponse<AuthResult>.FailureResponse("Email and OTP code are required", 400));
            }

            var cmd = new VerifyEmailCommand
            {
                Email = request.Email,
                OtpCode = request.OtpCode
            };

            var result = await _commands.Send<VerifyEmailCommand, AuthResult>(cmd, ct);

            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse<AuthResult>.FailureResponse(result.ErrorMessage ?? "Email verification failed", result.ErrorCode ?? 400));
            }

            return Ok(ApiResponse<AuthResult>.SuccessResponse(result.Data!, result.ErrorMessage ?? "Email verified successfully"));
        }

        /// <summary>
        /// Resends OTP for email verification.
        /// </summary>
        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<bool>.FailureResponse("Invalid input", 400));

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(ApiResponse<bool>.FailureResponse("Email is required", 400));
            }

            try
            {
                var cmd = new ResendOtpCommand
                {
                    Email = request.Email
                };

                var response = await _commands.Send<ResendOtpCommand, bool>(cmd, ct);

                if (!response.IsSuccess)
                {
                    // Return appropriate status code based on error
                    if (response.ErrorMessage?.Contains("wait") == true)
                        return StatusCode(StatusCodes.Status429TooManyRequests, response);
                    
                    if (response.ErrorMessage?.Contains("not found") == true)
                        return NotFound(response);

                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<bool>.FailureResponse("An unexpected error occurred", 500));
            }
        }

        /// <summary>
        /// Starts forgot-password flow by sending reset OTP.
        /// </summary>
        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<bool>.FailureResponse("Invalid input", 400));

            try
            {
                var cmd = new ForgetPasswordCommand
                {
                    Email = request.Email
                };

                var response = await _commands.Send<ForgetPasswordCommand, bool>(cmd, ct);
                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<bool>.FailureResponse("An unexpected error occurred", 500));
            }
        }

        /// <summary>
        /// Resets password using email + OTP.
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<bool>.FailureResponse("Invalid input", 400));

            try
            {
                var cmd = new ResetPasswordCommand
                {
                    Email = request.Email,
                    OtpCode = request.OtpCode,
                    NewPassword = request.NewPassword
                };

                var response = await _commands.Send<ResetPasswordCommand, bool>(cmd, ct);
                
                if (!response.IsSuccess)
                    return BadRequest(response);

                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<bool>.FailureResponse("An unexpected error occurred", 500));
            }
        }

        /// <summary>
        /// Changes password for the authenticated user.
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<bool>.FailureResponse("Invalid input", 400));

            try
            {
                // Get user ID from JWT claims
                if (!User.TryGetCurrentUserId(out var userId))
                {
                    return Unauthorized(ApiResponse<bool>.FailureResponse("Invalid or missing user token", 401));
                }

                var cmd = new ChangePasswordCommand
                {
                    UserId = userId,
                    OldPassword = request.OldPassword,
                    NewPassword = request.NewPassword
                };

                var response = await _commands.Send<ChangePasswordCommand, bool>(cmd, ct);
                
                if (!response.IsSuccess)
                    return BadRequest(response);

                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<bool>.FailureResponse("An unexpected error occurred", 500));
            }
        }

        /// <summary>
        /// Updates authenticated user's profile (basic + extended fields).
        /// </summary>
        /// <remarks>
        /// All fields are optional; only provided fields are updated.
        /// </remarks>
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateUserProfileRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<UserProfileResult>.FailureResponse("Invalid input", 400));

            // Get user ID from JWT claims
            if (!User.TryGetCurrentUserId(out var userId))
            {
                return Unauthorized(ApiResponse<UserProfileResult>.FailureResponse("Invalid or missing user token", 401));
            }

            var cmd = new UpdateUserProfileCommand
            {
                UserId = userId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Bio = request.Bio,
                Phone = request.Phone,
                Gender = request.Gender,
                DateOfBirth = request.DateOfBirth,
                ProfileImageUrl = request.ProfileImageUrl,
                BackgroundImageUrl = request.BackgroundImageUrl,
                Location = request.Location,
                Website = request.Website
            };

            var result = await _commands.Send<UpdateUserProfileCommand, UserProfileResult>(cmd, ct);
            
            if (!result.IsSuccess)
                return BadRequest(ApiResponse<UserProfileResult>.FailureResponse(result.ErrorMessage ?? "Profile update failed", result.ErrorCode ?? 400));

            return Ok(ApiResponse<UserProfileResult>.SuccessResponse(result.Data!, result.ErrorMessage ?? "Profile updated successfully"));
        }
    }
}

