using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Domain.Enums
{
    public enum CommentStatus
    {
        Active = 1,     
        Pending = 2,  
        Hidden = 3,
        Edited = 4,
        Banned = 5
    }
}
