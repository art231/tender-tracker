using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TenderTracker.API.Data;
using TenderTracker.API.Models;
using TenderTracker.API.Services;

namespace TenderTracker.API.BackgroundServices
{
    public class DocumentSyncBackgroundService : BackgroundService
    {
        private readonly ILogger<DocumentSyncBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly DocumentSyncSettings _settings;
        private readonly TimeSpan _syncInterval = TimeSpan.FromHours(2); // Каждые 2 часа
        private readonly TimeSpan _initialDelay = TimeSpan.FromSeconds(30);

        public DocumentSyncBackgroundService(
            ILogger<DocumentSyncBackgroundService> logger,
            IServiceProvider serviceProvider,
            IOptions<DocumentSyncSettings> settings)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _settings = settings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Document Sync Background Service is starting.");

            // Начальная задержка перед первым запуском
            await Task.Delay(_initialDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting scheduled document sync...");
                    await SyncDocumentsAsync(stoppingToken);
                    _logger.LogInformation("Scheduled document sync completed.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during scheduled document sync.");
                }

                // Ожидание следующего запуска
                _logger.LogInformation("Next sync will run in {Hours} hours.", _syncInterval.TotalHours);
                await Task.Delay(_syncInterval, stoppingToken);
            }

            _logger.LogInformation("Document Sync Background Service is stopping.");
        }

        private async Task SyncDocumentsAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            
            var documentService = scope.ServiceProvider.GetRequiredService<IDocumentService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Получаем настройки синхронизации
            if (!_settings.EnableAutoSync)
            {
                _logger.LogInformation("Auto sync is disabled in settings.");
                return;
            }

            // Получаем тендеры, требующие синхронизации документов
            var tendersToSync = await GetTendersForSyncAsync(dbContext, cancellationToken);
            
            if (!tendersToSync.Any())
            {
                _logger.LogInformation("No tenders require document sync.");
                return;
            }

            _logger.LogInformation("Found {Count} tenders requiring document sync.", tendersToSync.Count());

            int totalDocumentsSynced = 0;
            int totalTendersProcessed = 0;

            foreach (var tender in tendersToSync)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    _logger.LogInformation("Syncing documents for tender: {PurchaseNumber} (ID: {Id})", 
                        tender.PurchaseNumber, tender.Id);

                    // Создаем задачи на загрузку документов
                    var tasksCreated = await CreateDownloadTasksForTenderAsync(dbContext, tender);
                    
                    // Обрабатываем задачи
                    var documentsSynced = await ProcessDownloadTasksAsync(documentService, dbContext, tender, cancellationToken);
                    
                    totalDocumentsSynced += documentsSynced;
                    totalTendersProcessed++;

                    _logger.LogInformation("Synced {Count} documents for tender {PurchaseNumber}", 
                        documentsSynced, tender.PurchaseNumber);

                    // Задержка для соблюдения rate limit API
                    await Task.Delay(TimeSpan.FromSeconds(_settings.ApiDelaySeconds), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing documents for tender: {PurchaseNumber} (ID: {Id})", 
                        tender.PurchaseNumber, tender.Id);
                }
            }

            _logger.LogInformation("Sync completed. Processed: {Tenders}, Documents synced: {Documents}", 
                totalTendersProcessed, totalDocumentsSynced);
        }

        private async Task<List<FoundTender>> GetTendersForSyncAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
        {
            var query = dbContext.FoundTenders.AsQueryable();

            // Фильтруем по настройкам
            if (_settings.SyncOnlyCompatibleTenders)
            {
                query = query.Where(t => t.TechnologyAnalyses.Any(a => a.IsCompatible));
            }

            if (_settings.MinMatchScore > 0)
            {
                query = query.Where(t => t.TechnologyAnalyses.Any(a => a.MatchScore >= _settings.MinMatchScore));
            }

            // Исключаем тендеры, у которых уже есть документы (если не настроена повторная синхронизация)
            if (!_settings.ResyncExistingDocuments)
            {
                query = query.Where(t => !t.Documents.Any());
            }

            // Ограничиваем количество для одной синхронизации
            return await query
                .OrderByDescending(t => t.PublishDate)
                .Take(_settings.MaxTendersPerSync)
                .ToListAsync(cancellationToken);
        }

        private async Task<int> CreateDownloadTasksForTenderAsync(ApplicationDbContext dbContext, FoundTender tender)
        {
            var tasks = new List<DocumentDownloadTask>();

            // Создаем задачи в зависимости от настроек
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

            // Добавляем задачи для конкретных типов документов
            foreach (var docType in _settings.DocumentTypesToDownload)
            {
                tasks.Add(new DocumentDownloadTask
                {
                    TenderId = tender.Id,
                    DocType = docType,
                    Priority = "low",
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (tasks.Any())
            {
                await dbContext.DocumentDownloadTasks.AddRangeAsync(tasks);
                await dbContext.SaveChangesAsync();
            }

            return tasks.Count;
        }

        private async Task<int> ProcessDownloadTasksAsync(
            IDocumentService documentService, 
            ApplicationDbContext dbContext, 
            FoundTender tender,
            CancellationToken cancellationToken)
        {
            var tasks = await dbContext.DocumentDownloadTasks
                .Where(t => t.TenderId == tender.Id && t.Status == Models.TaskStatus.Pending)
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.CreatedAt)
                .ToListAsync(cancellationToken);

            if (!tasks.Any())
                return 0;

            int documentsDownloaded = 0;

            foreach (var task in tasks)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    // Обновляем статус задачи
                    task.Status = Models.TaskStatus.InProgress;
                    task.StartedAt = DateTime.UtcNow;
                    dbContext.DocumentDownloadTasks.Update(task);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    // Выполняем загрузку в зависимости от типа задачи
                    List<TenderDocument> downloadedDocuments = new();

                    if (task.DocType == "all")
                    {
                        downloadedDocuments = await documentService.DownloadAllDocumentsAsync(tender.Id);
                    }
                    else if (task.DocType == "notification")
                    {
                        var document = await documentService.GetNotificationDocumentAsync(tender.Id);
                        if (document != null)
                            downloadedDocuments.Add(document);
                    }
                    else
                    {
                        var document = await documentService.DownloadDocumentAsync(tender.Id, task.DocType);
                        if (document != null)
                            downloadedDocuments.Add(document);
                    }

                    // Обновляем статус задачи
                    task.Status = Models.TaskStatus.Completed;
                    task.CompletedAt = DateTime.UtcNow;
                    dbContext.DocumentDownloadTasks.Update(task);

                    documentsDownloaded += downloadedDocuments.Count;

                    _logger.LogDebug("Task completed: {DocType} for tender {TenderId}, downloaded {Count} documents",
                        task.DocType, tender.Id, downloadedDocuments.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing task: {DocType} for tender {TenderId}", 
                        task.DocType, tender.Id);

                    // Обновляем статус задачи с ошибкой
                    task.Status = Models.TaskStatus.Failed;
                    task.ErrorMessage = ex.Message;
                    task.RetryCount++;
                    
                    // Планируем повторную попытку, если не превышен лимит
                    if (task.RetryCount < _settings.MaxRetryCount)
                    {
                        task.NextRetryAt = DateTime.UtcNow.AddMinutes(_settings.RetryDelayMinutes);
                        task.Status = Models.TaskStatus.Pending;
                    }

                    dbContext.DocumentDownloadTasks.Update(task);
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return documentsDownloaded;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Document Sync Background Service is stopping.");
            await base.StopAsync(cancellationToken);
        }
    }

    public class DocumentSyncSettings
    {
        public bool EnableAutoSync { get; set; } = true;
        public bool DownloadAfterSearch { get; set; } = true;
        public int SyncIntervalHours { get; set; } = 2;
        public bool SyncOnlyCompatibleTenders { get; set; } = true;
        public int MinMatchScore { get; set; } = 60;
        public bool ResyncExistingDocuments { get; set; } = false;
        public int MaxTendersPerSync { get; set; } = 10;
        public bool DownloadNotification { get; set; } = true;
        public bool DownloadAllDocuments { get; set; } = false;
        public List<string> DocumentTypesToDownload { get; set; } = new List<string>();
        public int ApiDelaySeconds { get; set; } = 2;
        public int MaxRetryCount { get; set; } = 3;
        public int RetryDelayMinutes { get; set; } = 5;
    }
}
