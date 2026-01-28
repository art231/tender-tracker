using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using TenderTracker.API.Data;
using TenderTracker.API.Models;

namespace TenderTracker.API.BackgroundServices
{
    public class TenderCleanupBackgroundService : BackgroundService
    {
        private readonly ILogger<TenderCleanupBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromDays(1); // Ежедневно
        private readonly TimeSpan _initialDelay = TimeSpan.FromSeconds(30);

        public TenderCleanupBackgroundService(
            ILogger<TenderCleanupBackgroundService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Tender Cleanup Background Service is starting.");

            // Начальная задержка перед первым запуском
            await Task.Delay(_initialDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting scheduled tender cleanup...");
                    await CleanupExpiredTendersAsync(stoppingToken);
                    _logger.LogInformation("Scheduled tender cleanup completed.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during scheduled tender cleanup.");
                }

                // Ожидание следующего запуска (ежедневно)
                _logger.LogInformation("Next cleanup will run in {Days} days.", _cleanupInterval.TotalDays);
                await Task.Delay(_cleanupInterval, stoppingToken);
            }

            _logger.LogInformation("Tender Cleanup Background Service is stopping.");
        }

        private async Task CleanupExpiredTendersAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Удаляем тендеры, у которых срок подачи заявок истек более суток назад
            var cutoffDate = DateTime.UtcNow.AddDays(-1);
            
            var expiredTenders = await dbContext.FoundTenders
                .Where(t => t.ApplicationDeadline != null && t.ApplicationDeadline < cutoffDate)
                .ToListAsync(cancellationToken);
            
            if (!expiredTenders.Any())
            {
                _logger.LogInformation("No expired tenders found for cleanup.");
                return;
            }

            _logger.LogInformation("Found {Count} expired tenders for cleanup.", expiredTenders.Count);
            
            // Удаляем тендеры
            dbContext.FoundTenders.RemoveRange(expiredTenders);
            var deletedCount = await dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Successfully deleted {Count} expired tenders.", deletedCount);
            
            // Логируем информацию об удаленных тендерах
            foreach (var tender in expiredTenders.Take(10)) // Логируем первые 10 для примера
            {
                _logger.LogDebug("Deleted tender: {PurchaseNumber} (Deadline: {Deadline})", 
                    tender.PurchaseNumber, tender.ApplicationDeadline);
            }
            
            if (expiredTenders.Count > 10)
            {
                _logger.LogDebug("... and {Count} more expired tenders.", expiredTenders.Count - 10);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Tender Cleanup Background Service is stopping.");
            await base.StopAsync(cancellationToken);
        }
    }
}
