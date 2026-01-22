using AuthService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AuthService.Application.DTOs;
using AuthService.Application.DTOs.Request;
using AuthService.Application.DTOs.Response;
using AuthService.Application.Enums;
using AuthService.Application.Exceptions;
using AuthService.Application.Services.Auth.Commands;
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
                return BadRequest(ApiResponse<UserDto>.FailureResponse("Invalid input, please try again", 400));
            // Handle Login Process
            try
            {
                // Get IP address and User-Agent for security logging
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() 
                    ?? HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                    ?? "Unknown";
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                // create Login Command from Request
                var cmd = new LoginCommand
                {
                    EmailOrUsername = request.EmailOrUsername,
                    Password = request.Password,
                    IpAddress = ipAddress,
                    UserAgent = userAgent
                };

                // Send Login Command to Handler
                var response = await _commands.Send<LoginCommand, UserDto>(cmd, ct);

                // Note: All events (login.successful, login.failed) are published by LoginHandler via outbox pattern
                if (!response.Success)
                    return Unauthorized(response);

                return Ok(response);
            }
            catch (AuthException ex)
            {
                // Note: Login failed events are published by LoginHandler
                return Unauthorized(ApiResponse<UserDto>.FailureResponse(ex.Message, (int)ex.ErrorCode));
            }
            catch (Exception)
            {
                // Note: Login failed events are published by LoginHandler
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<UserDto>.FailureResponse("An unexpected error occurred", 500));
            }
        }

        // api/v1/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
        {
            // check model state validations
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<UserDto>.FailureResponse("Invalid input", 400));
            try
            {
                // Mapping from Request into Command
                var cmd = new RegisterCommand
                {
                    Username = request.Username,
                    Email = request.Email,
                    Password = request.Password,
                    FirstName = request.FirstName,
                    LastName = request.LastName
                };

                // Send Register Command to Handler
                var response = await _commands.Send<RegisterCommand, UserDto>(cmd, ct);

                // Note: All events (user.created, registration.failed) are published by RegisterHandler via outbox pattern
                if (!response.Success || response.Data == null)
                    return BadRequest(response);

                return Ok(response);
            }
            catch (AuthException ex)
            {
                // Note: Registration failed events are published by RegisterHandler
                var status = ex.ErrorCode == AuthErrorCode.UserAlreadyExists
                   ? StatusCodes.Status409Conflict
                   : StatusCodes.Status400BadRequest;

                return StatusCode(status, ApiResponse<UserDto>.FailureResponse(ex.Message, (int)ex.ErrorCode));
            }
            catch (Exception)
            {
                // Note: Registration failed events are published by RegisterHandler
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<UserDto>.FailureResponse("An unexpected error occurred", 500));
            }
        }

        // api/v1/auth/google-login
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<UserDto>.FailureResponse("Invalid input", 400));

            try
            {
                var cmd = new GoogleLoginCommand
                {
                    IdToken = request.IdToken
                };

                var response = await _commands.Send<GoogleLoginCommand, UserDto>(cmd, ct);

                // Note: All events (google.login.successful, google.login.failed) are published by GoogleLoginHandler via outbox pattern
                if (!response.Success)
                    return Unauthorized(response);

                return Ok(response);
            }
            catch (Exception)
            {
                // Note: Google login failed events are published by GoogleLoginHandler
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<UserDto>.FailureResponse("An unexpected error occurred", 500));
            }
        }

        // api/v1/auth/refresh-token
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<UserDto>.FailureResponse("Invalid input", 400));

            try
            {
                var cmd = new RefreshTokenCommand
                {
                    RefreshToken = request.RefreshToken
                };

                var response = await _commands.Send<RefreshTokenCommand, UserDto>(cmd, ct);

                if (!response.Success)
                {
                    return Unauthorized(response);
                }

                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<UserDto>.FailureResponse("An unexpected error occurred", 500));
            }
        }

        // api/v1/auth/verify-email
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<UserDto>.FailureResponse("Invalid input", 400));

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.OtpCode))
            {
                return BadRequest(ApiResponse<UserDto>.FailureResponse("Email and OTP code are required", 400));
            }

            try
            {
                var cmd = new VerifyEmailCommand
                {
                    Email = request.Email,
                    OtpCode = request.OtpCode
                };

                var response = await _commands.Send<VerifyEmailCommand, UserDto>(cmd, ct);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<UserDto>.FailureResponse("An unexpected error occurred", 500));
            }
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

                if (!response.Success)
                {
                    // Return appropriate status code based on error
                    if (response.Message?.Contains("wait") == true)
                        return StatusCode(StatusCodes.Status429TooManyRequests, response);
                    
                    if (response.Message?.Contains("not found") == true)
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
                var cmd = new ForgetPasswordRequestCommand
                {
                    Email = request.Email
                };

                var response = await _commands.Send<ForgetPasswordRequestCommand, bool>(cmd, ct);
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
                
                if (!response.Success)
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
        [Microsoft.AspNetCore.Authorization.Authorize]
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
                
                if (!response.Success)
                    return BadRequest(response);

                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<bool>.FailureResponse("An unexpected error occurred", 500));
            }
        }

        // api/v1/auth/profile (PUT - Edit Profile - First Name and Last Name)
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<UserDto>.FailureResponse("Invalid input", 400));

            try
            {
                // Get user ID from JWT claims
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                                  ?? User.FindFirst("sub")
                                  ?? User.Claims.FirstOrDefault(c => c.Type == "user_id");

                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(ApiResponse<UserDto>.FailureResponse("Invalid or missing user token", 401));
                }

                var cmd = new UpdateProfileCommand
                {
                    UserId = userId,
                    FirstName = request.FirstName,
                    LastName = request.LastName
                };

                var response = await _commands.Send<UpdateProfileCommand, UserDto>(cmd, ct);
                
                if (!response.Success)
                    return BadRequest(response);

                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<UserDto>.FailureResponse("An unexpected error occurred", 500));
            }
        }

        // api/v1/auth/profile/options (PUT - Edit Profile Options - Bio, Phone, Gender, DOB, Images)
        [HttpPut("profile/options")]
        [Authorize]
        public async Task<IActionResult> UpdateProfileOptions([FromBody] UpdateProfileOptionsRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<UserDto>.FailureResponse("Invalid input", 400));

            try
            {
                // Get user ID from JWT claims
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                                  ?? User.FindFirst("sub")
                                  ?? User.Claims.FirstOrDefault(c => c.Type == "user_id");

                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(ApiResponse<UserDto>.FailureResponse("Invalid or missing user token", 401));
                }

                var cmd = new UpdateProfileOptionsCommand
                {
                    UserId = userId,
                    Bio = request.Bio,
                    Phone = request.Phone,
                    Gender = request.Gender,
                    DateOfBirth = request.DateOfBirth,
                    ProfileImageUrl = request.ProfileImageUrl,
                    BackgroundImageUrl = request.BackgroundImageUrl,
                    Location = request.Location,
                    Website = request.Website
                };

                var response = await _commands.Send<UpdateProfileOptionsCommand, UserDto>(cmd, ct);
                
                if (!response.Success)
                    return BadRequest(response);

                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<UserDto>.FailureResponse("An unexpected error occurred", 500));
            }
        }
    }
}
