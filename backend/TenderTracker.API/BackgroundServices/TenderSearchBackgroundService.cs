using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TenderTracker.API.Clients;
using TenderTracker.API.Data;
using TenderTracker.API.Models;
using TenderTracker.API.Services;

namespace TenderTracker.API.BackgroundServices
{
    public class TenderSearchBackgroundService : BackgroundService
    {
        private readonly ILogger<TenderSearchBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly DocumentSyncSettings _settings;
        private readonly TimeSpan _searchInterval = TimeSpan.FromMinutes(30); // Каждые 30 минут
        private readonly TimeSpan _initialDelay = TimeSpan.FromSeconds(10);

        public TenderSearchBackgroundService(
            ILogger<TenderSearchBackgroundService> logger,
            IServiceProvider serviceProvider,
            IOptions<DocumentSyncSettings> settings)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _settings = settings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Tender Search Background Service is starting.");

            // Начальная задержка перед первым запуском
            await Task.Delay(_initialDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting scheduled tender search...");
                    await SearchTendersAsync(stoppingToken);
                    _logger.LogInformation("Scheduled tender search completed.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during scheduled tender search.");
                }

                // Ожидание следующего запуска
                _logger.LogInformation("Next search will run in {Minutes} minutes.", _searchInterval.TotalMinutes);
                await Task.Delay(_searchInterval, stoppingToken);
            }

            _logger.LogInformation("Tender Search Background Service is stopping.");
        }

        private async Task SearchTendersAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            
            var searchQueryService = scope.ServiceProvider.GetRequiredService<ISearchQueryService>();
            var foundTenderService = scope.ServiceProvider.GetRequiredService<IFoundTenderService>();
            var gosPlanApiClient = scope.ServiceProvider.GetRequiredService<IGosPlanApiClient>();
            var documentService = scope.ServiceProvider.GetRequiredService<IDocumentService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Получаем активные поисковые запросы
            var activeQueries = await searchQueryService.GetActiveQueriesAsync();
            
            if (!activeQueries.Any())
            {
                _logger.LogInformation("No active search queries found.");
                return;
            }

            _logger.LogInformation("Found {Count} active search queries.", activeQueries.Count());

            int totalTendersFound = 0;
            int totalTendersAdded = 0;

            foreach (var query in activeQueries)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    _logger.LogInformation("Searching tenders for query: {Keyword} (ID: {Id})", 
                        query.Keyword, query.Id);

                    // Поиск тендеров через API с расширенными параметрами
                    // Используем увеличенный лимит (100 вместо 50) и получаем все поля
                    var gosPlanTenders = await gosPlanApiClient.SearchTendersAdvancedAsync(
                        query.Keyword, 
                        query.Id,
                        limit: 100);
                    totalTendersFound += gosPlanTenders.Count;

                    if (!gosPlanTenders.Any())
                    {
                        _logger.LogInformation("No tenders found for query: {Keyword}", query.Keyword);
                        continue;
                    }

                    // Преобразуем в модель FoundTender с сохранением всех полей
                    var tendersToAdd = gosPlanTenders.Select(t => new FoundTender
                    {
                        ExternalId = t.ExternalId,
                        PurchaseNumber = t.PurchaseNumber,
                        Title = t.Title,
                        CustomerName = t.CustomerName,
                        PublishDate = t.PublishDate,
                        DirectLinkToSource = t.DirectLinkToSource,
                        FoundByQueryId = query.Id,
                        SavedAt = DateTime.UtcNow,
                        
                        // Новые поля
                        ApplicationDeadline = t.ApplicationDeadline,
                        MaxPrice = t.MaxPrice,
                        Region = t.Region,
                        CustomerInn = t.CustomerInn,
                        AdditionalInfo = t.AdditionalInfo,
                        
                        // Планы-графики (сохраняем как JSON)
                        PlanNumbersJson = t.PlanNumbers != null ? JsonSerializer.Serialize(t.PlanNumbers) : null
                    }).ToList();

                    // Добавляем тендеры в БД (с дедупликацией)
                    var addedCount = await foundTenderService.AddRangeAsync(tendersToAdd);
                    totalTendersAdded += addedCount;

                    _logger.LogInformation("Added {AddedCount} new tenders for query: {Keyword} (found: {TotalCount})",
                        addedCount, query.Keyword, gosPlanTenders.Count);

                    // Если включена автоматическая загрузка документов, создаем задачи на загрузку
                    if (_settings.EnableAutoSync && _settings.DownloadAfterSearch)
                    {
                        await CreateDocumentDownloadTasksForNewTendersAsync(dbContext, tendersToAdd, addedCount);
                    }

                    // Небольшая задержка между запросами для соблюдения rate limit
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error searching tenders for query: {Keyword} (ID: {Id})", 
                        query.Keyword, query.Id);
                }
            }

            _logger.LogInformation("Search completed. Found: {Found}, Added: {Added}", 
                totalTendersFound, totalTendersAdded);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Tender Search Background Service is stopping.");
            await base.StopAsync(cancellationToken);
        }

        private async Task CreateDocumentDownloadTasksForNewTendersAsync(
            ApplicationDbContext dbContext, 
            List<FoundTender> tendersToAdd, 
            int addedCount)
        {
            try
            {
                if (addedCount == 0)
                    return;

                var tasks = new List<DocumentDownloadTask>();
                var addedTenders = tendersToAdd.Take(addedCount).ToList();

                foreach (var tender in addedTenders)
                {
                    // Создаем задачи на загрузку документов в зависимости от настроек
                    if (_settings.DownloadNotification)
                    {
                        tasks.Add(new DocumentDownloadTask
                        {
                            TenderId = tender.Id,
                            DocType = "notification",
                            Priority = "high",
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    if (_settings.DownloadAllDocuments)
                    {
                        tasks.Add(new DocumentDownloadTask
                        {
                            TenderId = tender.Id,
                            DocType = "all",
                            Priority = "normal",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                if (tasks.Any())
                {
                    await dbContext.DocumentDownloadTasks.AddRangeAsync(tasks);
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Created {Count} document download tasks for new tenders", tasks.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document download tasks for new tenders");
            }
        }
    }
}
