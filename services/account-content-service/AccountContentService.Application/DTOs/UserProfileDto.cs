using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.DTOs
{
    public class UserProfileDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string AvatarUrl { get; set; }
    }
}
