using AccountContentService.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Interfaces.Services
{
    public interface IUserServiceClient
    {
        Task<UserProfileDto> GetMyProfile();
        Task<UserProfileDto> GetProfileByUserId(Guid userId);
    }
}
