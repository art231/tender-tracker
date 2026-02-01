using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TenderTracker.API.Data;
using TenderTracker.API.DTOs;
using TenderTracker.API.Models;
using TenderTracker.API.Services;

namespace TenderTracker.API.BackgroundServices
{
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly ILogger<NotificationBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Проверка каждые 5 минут

        public NotificationBackgroundService(
            ILogger<NotificationBackgroundService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationBackgroundService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessNotificationsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing notifications");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("NotificationBackgroundService stopped");
        }

        private async Task ProcessNotificationsAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificationSettingsService = scope.ServiceProvider.GetRequiredService<INotificationSettingsService>();
            var notificationSenderService = scope.ServiceProvider.GetRequiredService<INotificationSenderService>();

            // Получаем все активные настройки уведомлений
            var activeSettings = await notificationSettingsService.GetAllActiveAsync();
            
            if (!activeSettings.Any())
            {
                _logger.LogDebug("No active notification settings found");
                return;
            }

            _logger.LogInformation("Processing notifications for {Count} active users", activeSettings.Count());

            // 1. Проверяем новые тендеры (за последние 24 часа)
            await ProcessNewTendersNotificationsAsync(dbContext, activeSettings, notificationSettingsService, notificationSenderService, cancellationToken);

            // 2. Проверяем приближающиеся сроки подачи заявок
            await ProcessDeadlineNotificationsAsync(dbContext, activeSettings, notificationSenderService, cancellationToken);

            // 3. Проверяем совпадения технологий (новые анализы за последние 24 часа)
            await ProcessTechnologyMatchNotificationsAsync(dbContext, activeSettings, notificationSenderService, cancellationToken);
        }

        private async Task ProcessNewTendersNotificationsAsync(
            ApplicationDbContext dbContext,
            IEnumerable<NotificationSettingsDto> activeSettings,
            INotificationSettingsService notificationSettingsService,
            INotificationSenderService notificationSenderService,
            CancellationToken cancellationToken)
        {
            try
            {
                var last24Hours = DateTime.UtcNow.AddHours(-24);
                
                var newTenders = await dbContext.FoundTenders
                    .Include(t => t.FoundByQuery)
                    .Where(t => t.SavedAt >= last24Hours)
                    .OrderByDescending(t => t.SavedAt)
                    .Take(100) // Ограничиваем количество для обработки
                    .ToListAsync(cancellationToken);

                if (!newTenders.Any())
                {
                    _logger.LogDebug("No new tenders found in the last 24 hours");
                    return;
                }

                _logger.LogInformation("Found {Count} new tenders for notification processing", newTenders.Count);

                foreach (var settings in activeSettings.Where(s => s.NotifyOnNewTenders))
                {
                    foreach (var tender in newTenders)
                    {
                        try
                        {
                            var shouldNotify = await notificationSettingsService.ShouldNotifyForTenderAsync(settings.UserId, tender);
                            
                            if (shouldNotify)
                            {
                                var sent = await notificationSenderService.SendNewTenderNotificationAsync(settings, tender);
                                
                                if (sent)
                                {
                                    _logger.LogInformation("New tender notification sent to user {UserId} for tender {TenderId}", 
                                        settings.UserId, tender.Id);
                                    
                                    // Добавляем небольшую задержку между уведомлениями
                                    await Task.Delay(100, cancellationToken);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error sending new tender notification to user {UserId} for tender {TenderId}", 
                                settings.UserId, tender.Id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing new tenders notifications");
            }
        }

        private async Task ProcessDeadlineNotificationsAsync(
            ApplicationDbContext dbContext,
            IEnumerable<NotificationSettingsDto> activeSettings,
            INotificationSenderService notificationSenderService,
            CancellationToken cancellationToken)
        {
            try
            {
                var now = DateTime.UtcNow;
                var settingsWithDeadline = activeSettings.Where(s => s.NotifyOnDeadlineApproaching).ToList();

                if (!settingsWithDeadline.Any())
                    return;

                // Для каждого пользователя проверяем тендеры с приближающимся сроком
                foreach (var settings in settingsWithDeadline)
                {
                    try
                    {
                        var warningDays = settings.DeadlineWarningDays;
                        var warningDate = now.AddDays(warningDays);

                        var tendersWithApproachingDeadline = await dbContext.FoundTenders
                            .Include(t => t.FoundByQuery)
                            .Where(t => t.ApplicationDeadline.HasValue &&
                                       t.ApplicationDeadline > now &&
                                       t.ApplicationDeadline <= warningDate)
                            .OrderBy(t => t.ApplicationDeadline)
                            .Take(50) // Ограничиваем количество
                            .ToListAsync(cancellationToken);

                        if (!tendersWithApproachingDeadline.Any())
                            continue;

                        _logger.LogInformation("Found {Count} tenders with approaching deadline for user {UserId}", 
                            tendersWithApproachingDeadline.Count, settings.UserId);

                        foreach (var tender in tendersWithApproachingDeadline)
                        {
                            try
                            {
                                var sent = await notificationSenderService.SendDeadlineApproachingNotificationAsync(settings, tender);
                                
                                if (sent)
                                {
                                    _logger.LogInformation("Deadline notification sent to user {UserId} for tender {TenderId}", 
                                        settings.UserId, tender.Id);
                                    
                                    await Task.Delay(100, cancellationToken);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error sending deadline notification to user {UserId} for tender {TenderId}", 
                                    settings.UserId, tender.Id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing deadline notifications for user {UserId}", settings.UserId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing deadline notifications");
            }
        }

        private async Task ProcessTechnologyMatchNotificationsAsync(
            ApplicationDbContext dbContext,
            IEnumerable<NotificationSettingsDto> activeSettings,
            INotificationSenderService notificationSenderService,
            CancellationToken cancellationToken)
        {
            try
            {
                var last24Hours = DateTime.UtcNow.AddHours(-24);
                var settingsWithTechMatch = activeSettings.Where(s => s.NotifyOnTechnologyMatch).ToList();

                if (!settingsWithTechMatch.Any())
                    return;

                var newAnalyses = await dbContext.TechnologyAnalyses
                    .Include(a => a.Tender)
                    .Where(a => a.AnalyzedAt >= last24Hours && a.IsCompatible)
                    .OrderByDescending(a => a.AnalyzedAt)
                    .Take(50) // Ограничиваем количество
                    .ToListAsync(cancellationToken);

                if (!newAnalyses.Any())
                {
                    _logger.LogDebug("No new technology analyses found in the last 24 hours");
                    return;
                }

                _logger.LogInformation("Found {Count} new technology analyses for notification processing", newAnalyses.Count);

                foreach (var settings in settingsWithTechMatch)
                {
                    foreach (var analysis in newAnalyses)
                    {
                        try
                        {
                            var sent = await notificationSenderService.SendTechnologyMatchNotificationAsync(settings, analysis.Tender, analysis);
                            
                            if (sent)
                            {
                                _logger.LogInformation("Technology match notification sent to user {UserId} for analysis {AnalysisId}", 
                                    settings.UserId, analysis.Id);
                                
                                await Task.Delay(100, cancellationToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error sending technology match notification to user {UserId} for analysis {AnalysisId}", 
                                settings.UserId, analysis.Id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing technology match notifications");
            }
        }
    }
}
