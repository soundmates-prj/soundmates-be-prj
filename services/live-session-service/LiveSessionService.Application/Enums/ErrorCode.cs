namespace LiveSessionService.Application.Enums;

/// <summary>
/// Standard HTTP error codes as enum
/// Tránh magic numbers trong code
/// </summary>
public enum ErrorCode
{
    // 2xx Success
    Ok = 200,
    Created = 201,
    NoContent = 204,
    
    // 4xx Client Errors
    BadRequest = 400,
    Unauthorized = 401,
    Forbidden = 403,
    NotFound = 404,
    Conflict = 409,
    UnprocessableEntity = 422,
    
    // 5xx Server Errors
    InternalServerError = 500,
    NotImplemented = 501,
    ServiceUnavailable = 503
}
