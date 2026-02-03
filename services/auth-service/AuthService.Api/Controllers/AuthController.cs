using AuthService.Api.Models.Requests;
using AuthService.Api.Models.Requests.User;
using AuthService.Api.Models.Responses;
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
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ICommandDispatcher _commands;

        public AuthController(ICommandDispatcher commands)
        {
            _commands = commands;
        }

        // api/v1/auth/login
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

        // api/v1/auth/register
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

        // api/v1/auth/google-login
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

        // api/v1/auth/refresh-token
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

        // api/v1/auth/verify-email
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

        // api/v1/auth/resend-otp
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

        // api/v1/auth/forget-password
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

        // api/v1/auth/reset-password
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

        // api/v1/auth/change-password
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<bool>.FailureResponse("Invalid input", 400));

            try
            {
                // Get user ID from JWT claims
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                                  ?? User.FindFirst("sub")
                                  ?? User.Claims.FirstOrDefault(c => c.Type == "user_id");

                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
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

        // api/v1/auth/profile (PUT - Update Full Profile)
        // Merged endpoint: Updates both basic info (firstname, lastname) and extended profile fields
        // All fields are optional - only provided fields will be updated
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateUserProfileRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<UserProfileResult>.FailureResponse("Invalid input", 400));

            // Get user ID from JWT claims
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                              ?? User.FindFirst("sub")
                              ?? User.Claims.FirstOrDefault(c => c.Type == "user_id");

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
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

