using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string message)
            : base(message)
        {
        }
    }
}
