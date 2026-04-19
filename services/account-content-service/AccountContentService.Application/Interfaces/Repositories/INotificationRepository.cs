using AccountContentService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Interfaces.Repositories
{
    public interface INotificationRepository
    {
        Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken);

        Task<List<Notification>> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken);

        Task<List<Notification>> GetNotReadByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellatioToken);

        Task<List<Notification>> GetReadByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken);

        Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken);

        Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken);

        Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken);

        Task DeleteByReferenceAndTypeAsync(Guid referenceId, string type, string messageKeyword, CancellationToken cancellationToken);    }
}
