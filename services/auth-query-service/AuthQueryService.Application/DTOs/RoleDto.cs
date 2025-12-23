using AuthQueryService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthQueryService.Application.DTOs
{
    public class RoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public RoleDto() { }
        public RoleDto(UserRole role)
        {
            Id = role.Id;
            Name = role.Name;
        }
    }
}
