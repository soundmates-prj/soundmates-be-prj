using AccountContentService.Application.Abstractions;
using AccountContentService.Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead
{
    public class MarkAllNotificationsAsReadHandler
    : IRequestHandler<MarkAllNotificationsAsReadCommand, bool>
    {
        private readonly INotificationRepository _repository;
        private readonly ILogger<MarkAllNotificationsAsReadCommand> _logger;

        public MarkAllNotificationsAsReadHandler(
            INotificationRepository repository,
            ILogger<MarkAllNotificationsAsReadCommand> logger)
        {
            _repository = repository;
            _logger = logger;

        }

        public async Task<bool> Handle(
            MarkAllNotificationsAsReadCommand request,
            CancellationToken cancellationToken)
        {
            try
            {

            await _repository.MarkAllAsReadAsync(request.UserId, cancellationToken);

            return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read. UserId: {UserId}", request.UserId);
                throw new Exception($"An error occurred while marking the notification as read. UserId: {request.UserId}", ex);
            }
        }
    }
}
