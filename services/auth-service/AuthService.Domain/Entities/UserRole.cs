using System;
using System.Collections.Generic;

namespace AuthService.Domain.Entities;

public partial class UserRole
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
