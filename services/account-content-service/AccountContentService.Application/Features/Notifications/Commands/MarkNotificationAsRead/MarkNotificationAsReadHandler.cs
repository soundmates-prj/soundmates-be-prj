using AccountContentService.Application.Abstractions;
using AccountContentService.Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.Notifications.Commands.MarkNotificationAsRead
{
    public class MarkNotificationAsReadHandler
    : IRequestHandler<MarkNotificationAsReadCommand, bool>
    {
        private readonly INotificationRepository _repository;
        private readonly ILogger<MarkNotificationAsReadHandler> _logger;

        public MarkNotificationAsReadHandler(
            INotificationRepository repository, ILogger<MarkNotificationAsReadHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<bool> Handle(
            MarkNotificationAsReadCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                await _repository.MarkAsReadAsync(request.NotificationId, cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read. NotificationId: {NotificationId}", request.NotificationId);
                throw new Exception($"An error occurred while marking the notification as read. NotificationId: {request.NotificationId}", ex);
            }
           
        }
    }
}
