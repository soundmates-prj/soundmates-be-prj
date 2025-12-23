using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.Enums
{
    public enum AuthErrorCode
    {
        None = 0,
        InvalidCredentials = 1001,
        UserAlreadyExists = 1002,
        RegistrationFailed = 1003,
        Unknown = 1999
    }
}
