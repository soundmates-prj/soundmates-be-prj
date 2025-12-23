using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.DTOs.Response
{
    public class LoginReponse
    {
        public UserDto User { get; set; } = null!;
        public string Token { get; set; } = null!;
        public string? RefreshToken { get; set; }
    }
}
