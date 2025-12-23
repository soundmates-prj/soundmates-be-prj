using AuthQueryService.Application.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthQueryService.Application.Exceptions
{
    public class AuthException : Exception
    {
        public AuthErrorCode ErrorCode { get; }

        public AuthException(AuthErrorCode errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}

