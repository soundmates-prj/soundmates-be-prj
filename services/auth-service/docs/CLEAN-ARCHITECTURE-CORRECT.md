# Clean Architecture - DTOs & Data Flow Chính Xác

## ??? Layer Breakdown

```
???????????????????????????????????????????????????????
?                   API Layer                         ?
?  - Controllers                                      ?
?  - API Request DTOs  (t? client)                    ?
?  - API Response DTOs (v? client)                    ?
?  - ApiResponse<T> wrapper                           ?
???????????????????????????????????????????????????????
                      ? Maps
???????????????????????????????????????????????????????
?              Application Layer                      ?
?  - Commands/Queries                                 ?
?  - Command Handlers                                 ?
?  - Result<T> (business result)                      ?
?  - Application Service Interfaces                   ?
?    (VD: IPasswordHasher, IEmailService)             ?
???????????????????????????????????????????????????????
                      ? Uses
???????????????????????????????????????????????????????
?                 Domain Layer                        ?
?  - Entities (User, Role, Profile)                   ?
?  - Value Objects (Email, Password)                  ?
?  - Domain Rules (PasswordRule, EmailRule)           ?
?  - Domain Events                                    ?
?  - Domain Exceptions                                ?
?  - Repository Interfaces (IUserRepository)          ?
?  - KHÔNG CÓ Services implementations                ?
?  - KHÔNG CÓ DTOs                                    ?
?  - KHÔNG CÓ Infrastructure concerns                 ?
???????????????????????????????????????????????????????
                      ? Implements
???????????????????????????????????????????????????????
?            Infrastructure Layer                     ?
?  - Repository Implementations                       ?
?  - DbContext                                        ?
?  - External Service Implementations                 ?
?    (BcryptPasswordHasher, EmailService)             ?
?  - Message Bus                                      ?
???????????????????????????????????????????????????????
```

---

## ?? DTOs - Khi nào dùng gì?

### 1. API Request DTOs (API Layer)

**M?c ?ích:** Nh?n data t? HTTP request

**V? trí:** `AuthService.Api/DTOs/Request/`

**Ví d?:**
```csharp
// AuthService.Api/DTOs/Request/ChangePasswordRequest.cs
namespace AuthService.Api.DTOs.Request;

public sealed class ChangePasswordRequest
{
    [Required]
    public string OldPassword { get; set; } = null!;
    
    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = null!;
}
```

### 2. API Response DTOs (API Layer)

**M?c ?ích:** Tr? data v? client qua HTTP

**V? trí:** `AuthService.Api/DTOs/Response/`

**Ví d?:**
```csharp
// AuthService.Api/DTOs/Response/UserResponse.cs
namespace AuthService.Api.DTOs.Response;

public sealed class UserResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? RoleName { get; set; }
    public bool IsActive { get; set; }
}
```

### 3. ApiResponse Wrapper (API Layer)

**M?c ?ích:** Wrapper th?ng nh?t cho HTTP responses

**V? trí:** `AuthService.Api/Wrappers/`

```csharp
// AuthService.Api/Wrappers/ApiResponse.cs
namespace AuthService.Api.Wrappers;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public int? ErrorCode { get; set; }
    
    public static ApiResponse<T> Ok(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message };
    
    public static ApiResponse<T> Error(string message, int? code = null)
        => new() { Success = false, Message = message, ErrorCode = code };
}
```

### 4. Application Result (Application Layer)

**M?c ?ích:** K?t qu? x? lý business logic (KHÔNG ph? thu?c HTTP)

**V? trí:** `AuthService.Application/Results/`

```csharp
// AuthService.Application/Results/Result.cs
namespace AuthService.Application.Results;

public class Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }
    public int? ErrorCode { get; init; }
    
    public static Result<T> Success(T data) 
        => new() { IsSuccess = true, Data = data };
    
    public static Result<T> Failure(string message, int? code = null) 
        => new() { IsSuccess = false, ErrorMessage = message, ErrorCode = code };
}
```

### 5. Domain Entities (Domain Layer)

**M?c ?ích:** Business objects v?i behavior

**V? trí:** `AuthService.Domain/Entities/`

```csharp
// AuthService.Domain/Entities/User.cs
namespace AuthService.Domain.Entities;

public partial class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    // ...
    
    // Domain methods
    public void ChangePassword(string hashedPassword, IDateTimeProvider dateTimeProvider)
    {
        Password = hashedPassword;
        UpdatedAt = dateTimeProvider.UtcNow;
    }
}
```

---

## ?? Data Flow Example: Change Password

### Flow hoàn ch?nh:

```
1. CLIENT
   POST /api/auth/change-password
   Body: { oldPassword: "xxx", newPassword: "yyy" }

2. API LAYER (Controller)
   ? Nh?n ChangePasswordRequest (API DTO)
   ? Map sang ChangePasswordCommand
   ? G?i Handler

3. APPLICATION LAYER (Handler)
   ? Validate command
   ? Get User entity t? repository
   ? Hash password (dùng IPasswordHasher)
   ? G?i user.ChangePassword() (domain method)
   ? Save changes
   ? Return Result<bool>

4. API LAYER (Controller)
   ? Map Result<bool> ? ApiResponse<bool>
   ? Return HTTP 200 OK v?i ApiResponse

5. CLIENT
   Nh?n: { success: true, message: "...", data: true }
```

### Code implementation:

```csharp
// 1. API Request DTO
public class ChangePasswordRequest
{
    public string OldPassword { get; set; }
    public string NewPassword { get; set; }
}

// 2. Controller
[HttpPost("change-password")]
public async Task<IActionResult> ChangePassword(
    [FromBody] ChangePasswordRequest request, 
    CancellationToken ct)
{
    // Map Request DTO ? Command
    var command = new ChangePasswordCommand
    {
        UserId = GetCurrentUserId(),
        OldPassword = request.OldPassword,
        NewPassword = request.NewPassword
    };
    
    // Execute handler (Application layer)
    var result = await _commandDispatcher.Send<ChangePasswordCommand, bool>(command, ct);
    
    // Map Result ? ApiResponse
    var apiResponse = result.ToApiResponse();
    
    // Return HTTP response
    return apiResponse.Success 
        ? Ok(apiResponse) 
        : BadRequest(apiResponse);
}

// 3. Handler returns Result<bool>
public async Task<Result<bool>> Handle(ChangePasswordCommand command, ...)
{
    // Business logic
    var user = await _userRepo.GetByIdAsync(command.UserId);
    var hash = _passwordHasher.HashPassword(command.NewPassword);
    user.ChangePassword(hash, _dateTimeProvider);
    await _userRepo.UpdateAsync(user);
    
    return Result<bool>.Success(true);
}

// 4. Extension method
public static ApiResponse<T> ToApiResponse<T>(this Result<T> result)
{
    return result.IsSuccess
        ? ApiResponse<T>.Ok(result.Data, result.ErrorMessage)
        : ApiResponse<T>.Error(result.ErrorMessage, result.ErrorCode);
}
```

---

## ? CÓ trong Domain Layer

- ? Entities (User, Role, Profile)
- ? Value Objects (Email, Password)
- ? Domain Events (UserCreatedEvent)
- ? Domain Exceptions (UserNotFoundException)
- ? Domain Rules (PasswordRule, EmailRule)
- ? Repository Interfaces (IUserRepository)
- ? Enums used in business logic (Gender)

## ? KHÔNG có trong Domain Layer

- ? DTOs (Request/Response/Result)
- ? Service Implementations (BcryptPasswordHasher)
- ? Infrastructure concerns (DbContext, HTTP, RabbitMQ)
- ? External library dependencies (BCrypt.Net, Npgsql)
- ? Mappers (User ? UserDto)
- ? ApiResponse wrappers

---

## ?? Quy t?c vàng

### Request DTOs (API Layer)
- ? Validation attributes ([Required], [EmailAddress])
- ? Swagger documentation attributes
- ? JSON serialization attributes
- ? KHÔNG có business logic

### Commands (Application Layer)
- ? Immutable (record ho?c init-only properties)
- ? Ch?a data c?n ?? th?c thi use case
- ? KHÔNG có validation attributes
- ? KHÔNG có HTTP concerns

### Result (Application Layer)
- ? Success/Failure pattern
- ? Error codes (business codes, not HTTP codes)
- ? Có th? dùng cho gRPC, Console app, GraphQL
- ? KHÔNG có HTTP status codes

### ApiResponse (API Layer)
- ? HTTP status codes
- ? Consistent JSON structure
- ? Client-friendly error messages
- ? KHÔNG dùng trong Application layer

### Entities (Domain Layer)
- ? Business logic methods
- ? Domain validation
- ? Rich domain model
- ? KHÔNG có DTOs
- ? KHÔNG có annotations cho JSON/DB

---

## ?? Checklist refactoring

### Khi refactor m?t Handler:

- [ ] Handler return `Result<T>` (không ph?i `ApiResponse<T>`)
- [ ] Commands không có validation attributes
- [ ] Inject `IPasswordHasher` (không dùng BCrypt tr?c ti?p)
- [ ] S? d?ng Domain entity methods (user.ChangePassword())
- [ ] Không có HTTP concerns (status codes, headers)
- [ ] Handle exceptions ? return Result.Failure()

### Khi refactor m?t Controller:

- [ ] Nh?n Request DTO t? [FromBody]
- [ ] Map Request DTO ? Command
- [ ] G?i Handler qua Dispatcher
- [ ] Map Result<T> ? ApiResponse<T>
- [ ] Return IActionResult v?i HTTP status

---

## ?? Migration path

### B??c 1: Tách DTOs
```bash
# T?o folders
mkdir AuthService.Api/DTOs/Request
mkdir AuthService.Api/DTOs/Response

# Move ho?c t?o m?i Request/Response DTOs
# XÓA DTOs kh?i Application layer
```

### B??c 2: Move ApiResponse
```bash
# Move ApiResponse t? Application ? API layer
# Ho?c t?o m?i trong Api/Wrappers/
```

### B??c 3: Refactor Handlers
```bash
# Refactor t?ng handler:
# - Return Result<T>
# - Remove ApiResponse
# - Use IPasswordHasher
```

### B??c 4: Update Controllers
```bash
# Map Result ? ApiResponse trong Controllers
# Use extension method: result.ToApiResponse()
```

---

? **Bây gi? architecture s?ch và ?úng chu?n!**
