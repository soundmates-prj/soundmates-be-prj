using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.Payments.Commands.CallbackCommand
{
    public class VNPayCallbackCommand : IRequest<string>
    {
        public Dictionary<string, string> Data { get; set; } = new();
    }
}
