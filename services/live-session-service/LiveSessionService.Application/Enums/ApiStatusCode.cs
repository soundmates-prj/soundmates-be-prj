namespace LiveSessionService.Application.Enums;

public enum ApiStatusCode
{
    // 400 - Bad Request
    BadRequest = 40000,

    // 401 - Unauthorized
    Unauthorized = 40100,

    // 403 - Forbidden
    Forbidden = 40300,

    // 404 - Not Found
    NotFound = 40400,

    // 409 - Conflict
    Conflict = 40900,

    // 500 - Internal Server Error
    InternalError = 50000
}

