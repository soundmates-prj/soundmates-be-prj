# ? FIXED - Architecture Corrected

## ? V?n ?? ban ??u

Tôi ?ã **sai** khi ??t Service implementations trong Domain layer:
```
Domain/
  ??? Services/
      ??? IPasswordHasher.cs         ? Có th? có interface
      ??? BCryptPasswordHasher.cs    ?? SAI HOÀN TOÀN!
```

**Lý do sai:** Domain layer không ???c ph? thu?c vào external libraries (BCrypt.Net)

---

## ? ?ã s?a - Architecture ?úng

### 1. IPasswordHasher ? Application Layer
```
AuthService.Application/
  ??? Services/
      ??? IPasswordHasher.cs  ? Application service contract
```

### 2. BcryptPasswordHasher ? Infrastructure Layer  
```
AuthService.Infrastructure/
  ??? Services/
      ??? BcryptPasswordHasher.cs  ? Implementation
```

### 3. ?ã xóa:
- ? `Domain/Services/IPasswordHasher.cs` - Moved to Application
- ? `Domain/Services/BCryptPasswordHasher.cs` - Moved to Infrastructure
- ? `Domain/Mappers/UserMapper.cs` - Domain không nên có mappers

---

## ??? Clean Architecture - ?úng chu?n

```
???????????????????????????????????????
?         API Layer                   ?
?  ? Controllers                      ?
?  ? Request/Response DTOs            ?
?  ? ApiResponse<T> wrapper           ?
???????????????????????????????????????
              ?
???????????????????????????????????????
?      Application Layer              ?
?  ? Commands/Handlers                ?
?  ? Result<T> pattern                ?
?  ? Service Interfaces               ?
?     (IPasswordHasher, IEmailService) ?
???????????????????????????????????????
              ?
???????????????????????????????????????
?         Domain Layer                ?
?  ? Entities (User, Role)            ?
?  ? Value Objects (Email)            ?
?  ? Domain Rules (PasswordRule)      ?
?  ? Domain Events                    ?
?  ? Repository Interfaces            ?
?  ? NO Services implementations      ?
?  ? NO DTOs                          ?
?  ? NO External dependencies         ?
???????????????????????????????????????
              ?
???????????????????????????????????????
?     Infrastructure Layer            ?
?  ? Repository Implementations       ?
?  ? DbContext                        ?
?  ? Service Implementations          ?
?     (BcryptPasswordHasher)           ?
?  ? External integrations            ?
???????????????????????????????????????
```

---

## ?? DTOs & Data Flow - Correct

### API Request DTO (API Layer)
```csharp
// Api/DTOs/Request/ChangePasswordRequest.cs
public class ChangePasswordRequest
{
    [Required]
    public string OldPassword { get; set; }
    
    [Required]
    public string NewPassword { get; set; }
}
```

### Command (Application Layer)
```csharp
// Application/Services/Auth/Commands/ChangePasswordCommand.cs
public record ChangePasswordCommand : ICommand<bool>
{
    public Guid UserId { get; init; }
    public string OldPassword { get; init; } = null!;
    public string NewPassword { get; init; } = null!;
}
```

### Handler returns Result (Application Layer)
```csharp
// Application/Services/Auth/Handlers/ChangePasswordHandler.cs
public async Task<Result<bool>> Handle(ChangePasswordCommand command, ...)
{
    // Use IPasswordHasher (Application service)
    var hash = _passwordHasher.HashPassword(command.NewPassword);
    
    // Use Domain entity method
    user.ChangePassword(hash, _dateTimeProvider);
    
    return Result<bool>.Success(true);
}
```

### Controller maps Result ? ApiResponse (API Layer)
```csharp
// Api/Controllers/AuthController.cs
[HttpPost("change-password")]
public async Task<IActionResult> ChangePassword(
    [FromBody] ChangePasswordRequest request)
{
    var command = new ChangePasswordCommand { ... };
    var result = await _dispatcher.Send<ChangePasswordCommand, bool>(command);
    var apiResponse = result.ToApiResponse();
    
    return apiResponse.Success ? Ok(apiResponse) : BadRequest(apiResponse);
}
```

---

## ? Handlers ?ã refactor

1. ? **ChangePasswordHandler** - Hoàn ch?nh
2. ? **CreateUserHandler** - Hoàn ch?nh

**?ã s? d?ng:**
- ? `IPasswordHasher` (Application service)
- ? Domain entity methods (user.ChangePassword())
- ? Return `Result<T>` thay vì `ApiResponse<T>`

---

## ? TODO - 19 handlers còn l?i

V?n c?n refactor các handlers sau (s? d?ng pattern gi?ng ChangePasswordHandler):

- LoginHandler
- RegisterHandler
- GoogleLoginHandler  
- RefreshTokenHandler
- ForgetPasswordRequestHandler
- ResetPasswordHandler
- UpdateProfileHandler
- UpdateProfileOptionsHandler
- VerifyEmailHandler
- ResendOtpHandler
- BanUserHandler
- UnbanUserHandler
- DeactivateUserHandler
- DeleteUserHandler
- UpdateUserHandler
- CreateRoleHandler
- UpdateRoleHandler
- DeleteRoleHandler

---

## ?? Pattern ?? refactor

```csharp
// BEFORE ?
public async Task<ApiResponse<bool>> Handle(XCommand command, ...)
{
    var hash = BCrypt.Net.BCrypt.HashPassword(password);
    return ApiResponse<bool>.SuccessResponse(true);
}

// AFTER ?
private readonly IPasswordHasher _passwordHasher;  // Inject this

public async Task<Result<bool>> Handle(XCommand command, ...)
{
    var hash = _passwordHasher.HashPassword(password);
    return Result<bool>.Success(true);
}
```

---

## ?? Documents t?o

1. ? `docs/CLEAN-ARCHITECTURE-CORRECT.md` - **??C FILE NÀY**
   - Chi ti?t v? layers
   - DTOs breakdown
   - Data flow examples
   - Checklist

2. ? `AuthService.Application/Services/IPasswordHasher.cs` - Interface
3. ? `AuthService.Infrastructure/Services/BcryptPasswordHasher.cs` - Implementation
4. ? Updated DI registration

---

## ?? Next Steps

### Option 1: Ti?p t?c refactor (Recommended)

Refactor t?ng handler theo pattern trong `ChangePasswordHandler`:

```bash
# Xem guide
cat docs/CLEAN-ARCHITECTURE-CORRECT.md

# Auto refactor (partial)
.\scripts\refactor-handlers.ps1
```

### Option 2: Rollback ICommandHandler

N?u c?n code ch?y ngay:

```csharp
// File: ICommandHandler.cs
// Temporarily rollback
public interface ICommandHandler<in TCommand, TResponse>
{
    Task<ApiResponse<TResponse>> Handle(...);  // Use ApiResponse for now
}
```

---

## ? Architecture bây gi? ?úng!

- ? Domain layer s?ch - không có infrastructure concerns
- ? Application layer có service contracts (IPasswordHasher)
- ? Infrastructure layer có implementations (BcryptPasswordHasher)
- ? API layer ch?u trách nhi?m HTTP concerns
- ? Result pattern cho Application, ApiResponse cho API

---

?? **Foundation ?ã ???c s?a ?úng! B?n có th? refactor ti?p ho?c rollback ?? ship code!**

**Read:** `docs/CLEAN-ARCHITECTURE-CORRECT.md` ?? hi?u ??y ?? v? DTOs và data flow!
