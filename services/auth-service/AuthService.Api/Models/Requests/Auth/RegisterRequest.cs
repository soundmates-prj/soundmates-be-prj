using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Api.Models.Requests
{
    public class RegisterRequest
    {
        [Required]
        public required string Username { get; set; }
        [Required, EmailAddress]
        public required string Email { get; set; }
        [Required, MinLength(6)]
        public required string Password { get; set; }
        [Required, MaxLength(128)]
        public required string FirstName { get; set; }
        [Required, MaxLength(128)]
        public required string LastName { get; set; }
    }
}
