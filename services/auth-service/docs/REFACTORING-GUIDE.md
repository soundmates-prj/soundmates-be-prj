# Refactoring Guide - Clean Architecture

## ? ?ã hoàn thành

### 1. Tách bi?t Application Result và API Response

**Tr??c:**
```csharp
// Handler tr? v? ApiResponse (HTTP concern trong Application layer ?)
public async Task<ApiResponse<bool>> Handle(...)
```

**Sau:**
```csharp
// Handler tr? v? Result (Application concern ?)
public async Task<Result<bool>> Handle(...)
```

### 2. Domain Services cho Password Hashing

**Tr??c:**
```csharp
// Hash password trong Handler ?
var passwordHash = BCrypt.Net.BCrypt.HashPassword(command.NewPassword);
```

**Sau:**
```csharp
// S? d?ng Domain Service ?
var passwordHash = _passwordHasher.HashPassword(command.NewPassword);
```

### 3. Mapper trong Domain Layer

```csharp
// Domain/Mappers/UserMapper.cs
public static TDto ToDto<TDto>(this User user) where TDto : class, new()
```

### 4. Extension ?? map Result sang ApiResponse (API layer)

```csharp
// Api/Extensions/ResultExtensions.cs
public static ApiResponse<T> ToApiResponse<T>(this Result<T> result)
```

---

## ?? Files ?ã t?o/c?p nh?t

### ? ?ã t?o m?i:

1. `AuthService.Application/Results/Result.cs` - Generic result pattern
2. `AuthService.Application/Results/ChangePasswordResult.cs` - Specific result
3. `AuthService.Domain/Services/IPasswordHasher.cs` - Interface
4. `AuthService.Domain/Services/BCryptPasswordHasher.cs` - Implementation
5. `AuthService.Domain/Mappers/UserMapper.cs` - Domain mapper
6. `AuthService.Api/Extensions/ResultExtensions.cs` - API mapping

### ? ?ã c?p nh?t:

1. `AuthService.Application/Abstractions/Messaging/ICommandHandler.cs`
2. `AuthService.Application/Services/Auth/Handlers/ChangePasswordHandler.cs`
3. `AuthService.Infrastructure/DependencyInjection.cs` - Register IPasswordHasher

---

## ?? C?n refactor ti?p

### Các Handlers c?n update:

1. ? **ChangePasswordHandler** - DONE
2. ? **CreateUserHandler** - C?n refactor
3. ? **LoginHandler** - C?n refactor
4. ? **RegisterHandler** - C?n refactor
5. ? **ResetPasswordHandler** - C?n refactor
6. ? **UpdateUserHandler** - C?n refactor
7. ? **BanUserHandler** - C?n refactor
8. ? **UnbanUserHandler** - C?n refactor
9. ? **DeactivateUserHandler** - C?n refactor

### Controllers c?n update:

1. ? **UserController** - C?n map Result ? ApiResponse
2. ? **AuthController** - C?n map Result ? ApiResponse

---

## ?? H??ng d?n refactor t?ng b??c

### B??c 1: Update Handler signature

**Tr??c:**
```csharp
public async Task<ApiResponse<Guid>> Handle(CreateUserCommand command, ...)
{
    return ApiResponse<Guid>.SuccessResponse(user.Id, "Success!");
}
```

**Sau:**
```csharp
public async Task<Result<Guid>> Handle(CreateUserCommand command, ...)
{
    return Result<Guid>.Success(user.Id, "Success!");
}
```

### B??c 2: Thay BCrypt b?ng IPasswordHasher

**Tr??c:**
```csharp
var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
var isValid = BCrypt.Net.BCrypt.Verify(password, hash);
```

**Sau:**
```csharp
// Inject IPasswordHasher trong constructor
private readonly IPasswordHasher _passwordHasher;

// S? d?ng
var passwordHash = _passwordHasher.HashPassword(password);
var isValid = _passwordHasher.VerifyPassword(password, hash);
```

### B??c 3: Update Controller ?? map Result

**Tr??c:**
```csharp
var res = await _commands.Send<CreateUserCommand, Guid>(cmd, ct);
if (!res.Success) 
    return BadRequest(res);
return StatusCode(StatusCodes.Status201Created, res);
```

**Sau:**
```csharp
var result = await _commands.Send<CreateUserCommand, Guid>(cmd, ct);
var apiResponse = result.ToApiResponse();

if (!apiResponse.Success) 
    return BadRequest(apiResponse);
return StatusCode(StatusCodes.Status201Created, apiResponse);
```

---

## ??? Architecture Layers

```
???????????????????????????????????????
?         API Layer (HTTP)            ?
?  - Controllers                      ?
?  - Request/Response DTOs            ?
?  - Map Result ? ApiResponse         ?
???????????????????????????????????????
              ? uses
???????????????????????????????????????
?      Application Layer              ?
?  - Commands/Queries                 ?
?  - Handlers                         ?
?  - Application DTOs/Results         ?
?  - Mappers (optional)               ?
???????????????????????????????????????
              ? uses
???????????????????????????????????????
?         Domain Layer                ?
?  - Entities                         ?
?  - Domain Services                  ?
?  - Value Objects                    ?
?  - Domain Rules                     ?
?  - Domain Mappers                   ?
???????????????????????????????????????
              ? implements
???????????????????????????????????????
?     Infrastructure Layer            ?
?  - Repositories                     ?
?  - DbContext                        ?
?  - External Services                ?
?  - Domain Service Impl              ?
???????????????????????????????????????
```

---

## ? Checklist cho m?i Handler

- [ ] Return `Result<T>` instead of `ApiResponse<T>`
- [ ] Inject `IPasswordHasher` (n?u dùng password)
- [ ] S? d?ng Domain methods (`User.CreateAdminUser()`, `user.ChangePassword()`)
- [ ] Handle exceptions và return `Result.Failure()`
- [ ] Không có HTTP concerns (status codes, headers, etc.)

---

## ?? Benefits

1. **Separation of Concerns**: Application không bi?t v? HTTP
2. **Testability**: D? test Application layer mà không c?n mock HTTP
3. **Reusability**: Result có th? dùng cho gRPC, GraphQL, Console app
4. **Domain-Driven**: Business logic n?m trong Domain, không rò r? ra ngoài
5. **Clean Architecture**: Dependency ?úng h??ng (API ? App ? Domain)

---

## ?? Next Steps

1. Refactor t?t c? Handlers ?? return Result
2. Update Controllers ?? map Result ? ApiResponse
3. Update ICommandDispatcher ?? work v?i Result
4. Xóa các HTTP concerns kh?i Application layer
5. Move toàn b? BCrypt calls vào IPasswordHasher
