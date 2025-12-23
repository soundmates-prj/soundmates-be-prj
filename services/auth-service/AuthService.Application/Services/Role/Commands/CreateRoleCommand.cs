using AuthService.Application.Abstractions.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AuthService.Application.Services.Role.Commands
{
    public class CreateRoleCommand : ICommand<Guid>
    {
        public string Name { get; set; } = null!;

    }
}
