using System;
using System.Collections.Generic;

namespace AuthService.Domain.Entities;

public partial class Oauthaccount
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string Provider { get; set; } = null!;

    public string ProviderAccountId { get; set; } = null!;

    public virtual User? User { get; set; }
}
