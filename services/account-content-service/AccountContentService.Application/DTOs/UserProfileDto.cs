using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.DTOs
{
    public class UserProfileDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ProfileImageUrl { get; set; }
    }
}
