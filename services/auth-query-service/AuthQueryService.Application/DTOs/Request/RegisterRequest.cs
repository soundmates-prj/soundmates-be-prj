using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthQueryService.Application.DTOs.Request
{
    public class RegisterRequest
    {
        [Required]
        public string Username { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; }
        [Required, MinLength(6)]
        public string Password { get; set; }
        [Required, MaxLength(128)]
        public string FirstName { get; set; }
        [Required, MaxLength(128)]
        public string LastName { get; set; }
    }
}
