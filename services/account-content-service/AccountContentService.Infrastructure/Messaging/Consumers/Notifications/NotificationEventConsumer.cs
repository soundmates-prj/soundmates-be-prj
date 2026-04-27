using AccountContentService.Application.Interfaces;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using shared.Contracts.Events.Notifications;
using Shared.Contracts.Events.Auth;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace AccountContentService.Infrastructure.Messaging.Consumers.Notifications
{
    public class NotificationEventConsumer : IAsyncDisposable
    {
        private const string ExchangeName = "soundmates.events";
        private const string QueueName = "account-content.notification-events";

        private readonly INotificationRepository _repo;
        private readonly IServiceProvider _serviceProvider;
        private readonly RabbitMqOptions _options;
        private readonly ILogger<NotificationEventConsumer> _logger;
        private readonly ConnectionFactory _factory;

        private IConnection? _connection;
        private IChannel? _channel;
        private bool _started;

        public NotificationEventConsumer(
            IOptions<RabbitMqOptions> options,
            INotificationRepository repo,
            IServiceProvider serviceProvider,
            ILogger<NotificationEventConsumer> logger)
        {
            _options = options.Value;
            _repo = repo;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_started) return;

            _connection = await _factory.CreateConnectionAsync(
                clientProvidedName: "account-content-service-consumer",
                cancellationToken);

            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            // Declare exchange
            await _channel.ExchangeDeclareAsync(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            // Declare queue
            var queueArgs = new Dictionary<string, object?>();
            await _channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs,
                cancellationToken: cancellationToken);

            // Bind queue to exchange with wildcard routing key (all user events)
            await _channel.QueueBindAsync(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: "notification.#", 
                cancellationToken: cancellationToken);

            // Set prefetch count (process 10 messages at a time)
            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: cancellationToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                var routingKey = ea.RoutingKey;
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                try
                {
                    await ProcessMessageAsync(routingKey, json, cancellationToken);
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process message with routing key {RoutingKey}: {Json}", routingKey, json);
                    // Negative ack — requeue once then move to DLQ
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: cancellationToken);

            _started = true;
            _logger.LogInformation(
                "UserEventConsumer started. Listening on queue {Queue} bound to exchange {Exchange}",
                QueueName, ExchangeName);
        }

        private async Task ProcessMessageAsync(string routingKey, string json, CancellationToken ct)
        {
            switch (routingKey)
            {
                case "notification.created":
                    await HandleNotificationCreatedAsync(json, ct);
                    break;

                default:
                    _logger.LogDebug("Ignoring event with routing key {RoutingKey}", routingKey);
                    break;
            }
        }

        private async Task HandleNotificationCreatedAsync(
            string json,
            CancellationToken cancellationToken)
        {
            var evt = JsonSerializer.Deserialize<NotificationEvent>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (evt == null)
            {
                _logger.LogWarning("Failed to deserialize UserCreatedEvent: {Json}", json);
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var notificationPusher = scope.ServiceProvider.GetService<INotificationPusher>();

            // ── BROADCAST: notify ALL connected users (e.g. new broadcast schedule) ──
            if (evt.IsBroadcast)
            {
                var baseGuid = Guid.NewGuid();
                var dbContext = scope.ServiceProvider.GetService<AccountContentDbContext>();
                if (dbContext != null)
                {
                    var allUserIds = dbContext.UserProfileReadModels.Select(x => x.Id).ToList();
                    var notifications = allUserIds.Select(userId => new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Title = evt.Title,
                        ReferenceId = evt.ReferenceId,
                        Type = evt.Type,
                        Message = evt.Message,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    }).ToList();

                    if (notifications.Count > 0)
                    {
                        dbContext.Notifications.AddRange(notifications);
                        await dbContext.SaveChangesAsync(cancellationToken);
                    }
                }

                var broadcastPayload = new
                {
                    Id = baseGuid,
                    Title = evt.Title,
                    Message = evt.Message,
                    Type = evt.Type,
                    ReferenceId = evt.ReferenceId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                if (notificationPusher != null)
                {
                    try
                    {
                        await notificationPusher.PushToAllAsync(broadcastPayload, cancellationToken);
                        _logger.LogInformation("Broadcast notification sent: {Title}", evt.Title);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to broadcast notification: {Title}", evt.Title);
                    }
                }
                return;
            }

            // ── PERSONAL: persist & push to specific user ──
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = evt.ReceiveUserId,
                Title = evt.Title,
                ReferenceId = evt.ReferenceId,
                Type = evt.Type,
                Message = evt.Message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(notification, cancellationToken);

            // Push notification to user in real-time via SignalR
            if (notificationPusher != null)
            {
                try
                {
                    await notificationPusher.PushToUserAsync(evt.ReceiveUserId, notification, cancellationToken);
                    _logger.LogInformation("Pushed notification {NotificationId} to user {UserId}", notification.Id, evt.ReceiveUserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to push notification {NotificationId} to user {UserId}", notification.Id, evt.ReceiveUserId);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel != null) await _channel.CloseAsync();
            _channel?.Dispose();
            if (_connection != null) await _connection.CloseAsync();
            _connection?.Dispose();
        }
    }
}
