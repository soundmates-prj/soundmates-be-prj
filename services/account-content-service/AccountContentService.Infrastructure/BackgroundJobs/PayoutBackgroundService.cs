using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using AccountContentService.Application.Abstractions;
using AccountContentService.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AccountContentService.Infrastructure.BackgroundJobs;

public class PayoutBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PayoutBackgroundService> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(30); // Check every 30 seconds

    public PayoutBackgroundService(IServiceProvider serviceProvider, ILogger<PayoutBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PayoutBackgroundService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingPayoutsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred processing pending payouts.");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("PayoutBackgroundService is stopping.");
    }

    private async Task ProcessPendingPayoutsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var pendingPayoutRepo = scope.ServiceProvider.GetRequiredService<IPendingPayoutRepository>();

        var now = DateTime.UtcNow;
        var duePayouts = await pendingPayoutRepo.GetPendingPayoutsAsync(now, cancellationToken);

        var duePayoutsList = duePayouts.ToList();

        if (!duePayoutsList.Any()) return;

        _logger.LogInformation("Found {Count} pending payouts to process.", duePayoutsList.Count);

        foreach (var payout in duePayoutsList)
        {
            if (cancellationToken.IsCancellationRequested) break;

            _logger.LogInformation("Processing mock payout {Id} for amount {Amount} to {AccountName} ({BankId})", payout.Id, payout.Amount, payout.AccountName, payout.BankId);

            try
            {
                // MOCK PAYOUT LOGIC
                // We simulate an outbound bank transfer delay
                await Task.Delay(1000, cancellationToken);

                payout.Status = "completed";
                payout.ExecutedAt = DateTime.UtcNow;
                _logger.LogInformation("Mock Payout {Id} completed successfully.", payout.Id);
            }
            catch (Exception ex)
            {
                payout.Status = "failed";
                payout.ErrorMessage = ex.Message;
                payout.UpdatedAt = DateTime.UtcNow;
                _logger.LogWarning(ex, "Mock Payout {Id} failed.", payout.Id);
            }

            await pendingPayoutRepo.UpdateAsync(payout);
        }
    }
}
