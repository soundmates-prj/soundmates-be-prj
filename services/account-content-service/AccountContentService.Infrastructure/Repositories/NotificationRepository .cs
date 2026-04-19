using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AccountContentDbContext _context;

        public NotificationRepository(AccountContentDbContext context)
        {
            _context = context;
        }

        public async Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken)
        {
            await _context.Notifications.AddAsync(notification, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return notification;
        }

        public async Task<List<Notification>> GetByUserIdAsync(
            Guid userId,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            return await _context.Notifications
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Notification>> GetNotReadByUserIdAsync(
            Guid userId,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            return await _context.Notifications
                .Where(x => x.UserId == userId && !x.IsRead)
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Notification>> GetReadByUserIdAsync(
            Guid userId,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            return await _context.Notifications
                .Where(x => x.UserId == userId && x.IsRead)
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await _context.Notifications
                .CountAsync(x => x.UserId == userId, cancellationToken);
        }

        public async Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(x => x.Id == notificationId, cancellationToken);

            if (notification == null) return;

            notification.IsRead = true;
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken)
        {
            var notifications = await _context.Notifications
                .Where(x => x.UserId == userId && !x.IsRead)
                .ToListAsync(cancellationToken);

            foreach (var noti in notifications)
            {
                noti.IsRead = true;
                _context.Notifications.Update(noti);
            }
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteByReferenceAndTypeAsync(Guid referenceId, string type, string messageKeyword, CancellationToken cancellationToken)
        {
            var query = _context.Notifications
                .Where(x => x.ReferenceId == referenceId && x.Type == type);
                
            if (!string.IsNullOrEmpty(messageKeyword))
            {
                query = query.Where(x => x.Message.Contains(messageKeyword));
            }

            var notifications = await query.ToListAsync(cancellationToken);

            if (notifications.Any())
            {
                _context.Notifications.RemoveRange(notifications);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
