namespace AuthService.Domain.Errors;

/// <summary>
/// Error codes for role-related operations
/// Centralized constants for role validation, state, and not found errors
/// </summary>
public static class RoleErrorCodes
{
    // Validation errors (400 Bad Request)
    public const string RoleNameEmpty = "ROLE_NAME_EMPTY";
    public const string RoleNameTooShort = "ROLE_NAME_TOO_SHORT";
    public const string RoleNameTooLong = "ROLE_NAME_TOO_LONG";
    public const string RoleNameInvalid = "ROLE_NAME_INVALID";
    
    // Not found errors (404 Not Found)
    public const string RoleNotFound = "ROLE_NOT_FOUND";
    
    // State transition errors (409 Conflict)
    public const string RoleAlreadyExists = "ROLE_ALREADY_EXISTS";
    public const string RoleInUse = "ROLE_IN_USE";
    public const string CannotDeleteSystemRole = "ROLE_CANNOT_DELETE_SYSTEM_ROLE";
}
