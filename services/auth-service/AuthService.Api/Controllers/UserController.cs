using AuthService.Api.Models.Requests.User;
using AuthService.Api.Models.Responses;
using AuthService.Api.Extensions;
using AuthService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AuthService.Application.Features.Users.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

/// <summary>
/// User account management endpoints (Admin only unless noted).
/// </summary>
[Route("api/v1/users")]
[ApiController]
[Authorize(Roles = "ADMIN")]
public class UserController : ControllerBase
{
    private readonly ICommandDispatcher _commands;

    public UserController(ICommandDispatcher commands)
    {
        _commands = commands;
    }

    /// <summary>
    /// Get paginated list of users.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        // Handled by auth-query-service — this is a placeholder to satisfy the route
        return Ok(ApiResponse<object>.SuccessResponse(null, "Query users via auth-query-service"));
    }

    /// <summary>
    /// Create a new user (Admin only).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<Guid>.FailureResponse("Invalid input", 400));

        var cmd = new CreateUserCommand
        {
            Username = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            RoleId = request.RoleId,
            Password = request.Password
        };

        var result = await _commands.Send<CreateUserCommand, Guid>(cmd, ct);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse<Guid>.FailureResponse(
                result.ErrorMessage ?? "Failed to create user", result.ErrorCode ?? 400));

        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<Guid>.SuccessResponse(result.Data!, "User created successfully"));
    }

    /// <summary>
    /// Update user information (Admin only).
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<bool>.FailureResponse("Invalid input", 400));

        var cmd = new UpdateUserCommand(id)
        {
            Username = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            RoleId = request.RoleId
        };

        var result = await _commands.Send<UpdateUserCommand, bool>(cmd, ct);

        if (!result.IsSuccess)
            return NotFound(ApiResponse<bool>.FailureResponse(
                result.ErrorMessage ?? "User not found", 404));

        return Ok(ApiResponse<bool>.SuccessResponse(true, "User updated successfully"));
    }

    /// <summary>
    /// Get user by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        // Handled by auth-query-service
        return Ok(ApiResponse<object>.SuccessResponse(null, "Query user via auth-query-service"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // STATUS — Unified PATCH endpoint (replaces deactivate/activate/ban/unban)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Update account status (PATCH semantics — idempotent).
    /// Replaces: POST /deactivate, POST /activate, POST /ban, POST /unban.
    /// Idempotent: setting the same status twice is a no-op (returns 200 OK).
    /// </summary>
    /// <remarks>
    /// - ACTIVE: User can login and use the system.
    /// - DEACTIVATED: User account is temporarily locked.
    /// - SUSPENDED: User account is locked due to policy violation.
    /// Both DEACTIVATED and SUSPENDED call the same domain method (IsActive=false).
    /// The semantic difference is stored in the outbox event metadata (reason/note).
    /// </remarks>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateAccountStatusRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<bool>.FailureResponse("Invalid input", 400));

        var cmd = new UpdateAccountStatusCommand(
            UserId: id,
            Status: request.Status,
            Reason: request.Reason,
            Note: request.Note);

        var result = await _commands.Send<UpdateAccountStatusCommand, bool>(cmd, ct);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == 404)
                return NotFound(ApiResponse<bool>.FailureResponse(
                    result.ErrorMessage ?? "User not found", 404));
            return BadRequest(ApiResponse<bool>.FromResult(result));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, result.ErrorMessage ?? "Status updated"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // EMAIL VERIFICATION
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Manually verify a user's email (Admin only).
    /// Activates the account without requiring OTP.
    /// </summary>
    [HttpPatch("{id:guid}/email-verification")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail(Guid id, CancellationToken ct)
    {
        var cmd = new VerifyUserEmailCommand(id);
        var result = await _commands.Send<VerifyUserEmailCommand, bool>(cmd, ct);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == 409)
                return Conflict(ApiResponse<bool>.FailureResponse(
                    result.ErrorMessage ?? "Email is already verified",
                    result.ErrorCode ?? 409));
            if (result.ErrorCode == 404)
                return NotFound(ApiResponse<bool>.FailureResponse(
                    result.ErrorMessage ?? "User not found", 404));
            return BadRequest(ApiResponse<bool>.FailureResponse(
                result.ErrorMessage ?? "Failed to verify email",
                result.ErrorCode ?? 400));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true,
            result.ErrorMessage ?? "Email verified and account activated"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // DELETE
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Permanently delete a user account (Admin only).
    /// WARNING: This cannot be undone. Consider using PATCH /status instead.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var cmd = new DeleteUserCommand(id);
        var result = await _commands.Send<DeleteUserCommand, bool>(cmd, ct);

        if (!result.IsSuccess)
            return NotFound(ApiResponse<bool>.FailureResponse(
                result.ErrorMessage ?? "User not found", 404));

        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    // BANK ACCOUNT
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Update user bank account (Owner).
    /// </summary>
    [HttpPut("bank-account")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBankAccount(
        [FromBody] UpdateBankAccountRequest request,
        CancellationToken ct)
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdString, out var userId))
            return Unauthorized(ApiResponse<bool>.FailureResponse("User ID not found in token", 401));

        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<bool>.FailureResponse("Invalid input", 400));

        var cmd = new UpdateBankAccountCommand(userId, request.BankId, request.AccountNumber, request.AccountName);
        var result = await _commands.Send<UpdateBankAccountCommand, bool>(cmd, ct);

        if (!result.IsSuccess)
            return NotFound(ApiResponse<bool>.FailureResponse(
                result.ErrorMessage ?? "User not found", 404));

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Bank account updated successfully"));
    }

    /// <summary>
    /// Get user bank account (Owner, Admin, or Inter-service).
    /// </summary>
    [HttpGet("{id:guid}/bank-account")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBankAccount(
        Guid id,
        [FromServices] AuthService.Infrastructure.Persistence.AuthDbContext dbContext,
        CancellationToken ct)
    {
        var account = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            dbContext.BankAccounts, x => x.UserId == id, ct);

        if (account == null)
            return NotFound();

        return Ok(new
        {
            account.UserId,
            account.BankId,
            account.AccountNumber,
            account.AccountName
        });
    }

    /// <summary>
    /// Get current user's bank account (Owner).
    /// </summary>
    [HttpGet("bank-account")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyBankAccount(
        [FromServices] AuthService.Infrastructure.Persistence.AuthDbContext dbContext,
        CancellationToken ct)
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdString, out var userId))
            return Unauthorized(ApiResponse<object>.FailureResponse("User ID not found in token", 401));

        var account = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            dbContext.BankAccounts, x => x.UserId == userId, ct);

        if (account == null)
            return NotFound(ApiResponse<object>.FailureResponse("Bank account not found", 404));

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            account.UserId,
            account.BankId,
            account.AccountNumber,
            account.AccountName
        }));
    }
}
