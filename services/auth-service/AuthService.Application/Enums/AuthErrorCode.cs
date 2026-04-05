using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.Enums
{
    /// <summary>
    /// Application-level error codes for authentication operations
    /// 
    /// NOTE: Domain exceptions should use Domain error codes (UserErrorCodes, RoleErrorCodes, etc.)
    /// These are application-specific errors that don't belong in the domain layer
    /// </summary>
    public enum AuthErrorCode
    {
        None = 0,
        
        // Authentication errors (1000-1099)
        InvalidCredentials = 1001,
        EmailNotVerified = 1002,
        AccountInactive = 1003,
        InvalidToken = 1004,
        TokenExpired = 1005,
        
        // Registration errors (1100-1199)
        UserAlreadyExists = 1101,
        RegistrationFailed = 1102,
        
        // Password errors (1200-1299)
        PasswordResetFailed = 1201,
        InvalidResetToken = 1202,
        OldPasswordIncorrect = 1203,
        
        // OTP errors (1300-1399)
        OtpExpired = 1301,
        OtpInvalid = 1302,
        OtpAlreadyUsed = 1303,
        
        // External auth errors (1400-1499)
        GoogleAuthFailed = 1401,

        // External service errors (1500-1599)
        EmailSendFailed = 1501,

        // Account status errors (1600-1699)
        AccountLocked = 1601,
        AccountBanned = 1602,

        // Generic errors
        Unknown = 1999
    }
}
