using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Api.Models.Requests
{
    public class LoginRequest
    {
        [Required]
        public string EmailOrUsername { get; set; } = null!;
        [Required]
        public string Password { get; set; } = null!;

        /// <summary>
        /// If true, issues a longer-lived refresh token (30 days instead of 7).
        /// </summary>
        public bool RememberMe { get; set; } = false;
    }
}
