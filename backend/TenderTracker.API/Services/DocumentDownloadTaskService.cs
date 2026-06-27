using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TenderTracker.API.Data;
using TenderTracker.API.DTOs;
using TenderTracker.API.Models;

namespace TenderTracker.API.Services
{
    public interface IDocumentDownloadTaskService
    {
        Task<DocumentDownloadTaskDto?> GetTaskByIdAsync(int taskId);
        Task<List<DocumentDownloadTaskDto>> GetTasksAsync(int? tenderId = null, string? status = null, string? docType = null);
        Task<DocumentDownloadTaskDto> CreateTaskAsync(CreateDocumentDownloadTaskDto createDto);
        Task<DocumentDownloadTaskDto?> UpdateTaskAsync(int taskId, UpdateDocumentDownloadTaskDto updateDto);
        Task<bool> DeleteTaskAsync(int taskId);
        Task<bool> RetryTaskAsync(int taskId);
        Task<bool> CancelTaskAsync(int taskId);
        Task<DocumentDownloadTaskStatsDto> GetStatsAsync();
        Task<List<DocumentDownloadTaskDto>> GetPendingTasksAsync(int limit = 100);
        Task<bool> ProcessTaskAsync(int taskId);
    }

    public class DocumentDownloadTaskService : IDocumentDownloadTaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DocumentDownloadTaskService> _logger;
        private readonly IDocumentService _documentService;

        public DocumentDownloadTaskService(
            ApplicationDbContext context,
            ILogger<DocumentDownloadTaskService> logger,
            IDocumentService documentService)
        {
            _context = context;
            _logger = logger;
            _documentService = documentService;
        }

        public async Task<DocumentDownloadTaskDto?> GetTaskByIdAsync(int taskId)
        {
            try
            {
                var task = await _context.DocumentDownloadTasks
                    .Include(t => t.Tender)
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task == null)
                    return null;

                return MapToDto(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task by ID: {TaskId}", taskId);
                throw;
            }
        }

        public async Task<List<DocumentDownloadTaskDto>> GetTasksAsync(int? tenderId = null, string? status = null, string? docType = null)
        {
            try
            {
                var query = _context.DocumentDownloadTasks
                    .Include(t => t.Tender)
                    .AsQueryable();

                if (tenderId.HasValue)
                {
                    query = query.Where(t => t.TenderId == tenderId.Value);
                }

                if (!string.IsNullOrEmpty(status))
                {
                    if (Enum.TryParse<Models.TaskStatus>(status, out var statusEnum))
                    {
                        query = query.Where(t => t.Status == statusEnum);
                    }
                }

                if (!string.IsNullOrEmpty(docType))
                {
                    query = query.Where(t => t.DocType == docType);
                }

                var tasks = await query
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                return tasks.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks");
                throw;
            }
        }

        public async Task<DocumentDownloadTaskDto> CreateTaskAsync(CreateDocumentDownloadTaskDto createDto)
        {
            try
            {
                // Проверяем существование тендера
                var tender = await _context.FoundTenders.FindAsync(createDto.TenderId);
                if (tender == null)
                {
                    throw new ArgumentException($"Tender with ID {createDto.TenderId} not found");
                }

                // Проверяем, не существует ли уже такая задача
                var existingTask = await _context.DocumentDownloadTasks
                    .FirstOrDefaultAsync(t => t.TenderId == createDto.TenderId && 
                                              t.DocType == createDto.DocType && 
                                              t.Status != Models.TaskStatus.Completed &&
                                              t.Status != Models.TaskStatus.Cancelled);

                if (existingTask != null)
                {
                    throw new InvalidOperationException($"Active task already exists for tender {createDto.TenderId} and document type {createDto.DocType}");
                }

                var task = new DocumentDownloadTask
                {
                    TenderId = createDto.TenderId,
                    DocType = createDto.DocType,
                    Priority = createDto.Priority ?? "normal",
                    CreatedAt = DateTime.UtcNow,
                    Status = Models.TaskStatus.Pending
                };

                _context.DocumentDownloadTasks.Add(task);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created document download task: ID={TaskId}, Tender={TenderId}, Type={DocType}",
                    task.Id, task.TenderId, task.DocType);

                return MapToDto(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document download task");
                throw;
            }
        }

        public async Task<DocumentDownloadTaskDto?> UpdateTaskAsync(int taskId, UpdateDocumentDownloadTaskDto updateDto)
        {
            try
            {
                var task = await _context.DocumentDownloadTasks
                    .Include(t => t.Tender)
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task == null)
                    return null;

                if (!string.IsNullOrEmpty(updateDto.Priority))
                {
                    task.Priority = updateDto.Priority;
                }

                if (!string.IsNullOrEmpty(updateDto.Status))
                {
                    if (Enum.TryParse<Models.TaskStatus>(updateDto.Status, out var statusEnum))
                    {
                        task.Status = statusEnum;
                        
                        if (statusEnum == Models.TaskStatus.InProgress)
                        {
                            task.StartedAt = DateTime.UtcNow;
                        }
                        else if (statusEnum == Models.TaskStatus.Completed || 
                                 statusEnum == Models.TaskStatus.Cancelled)
                        {
                            task.CompletedAt = DateTime.UtcNow;
                        }
                    }
                }

                _context.DocumentDownloadTasks.Update(task);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated document download task: ID={TaskId}", taskId);

                return MapToDto(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task: {TaskId}", taskId);
                throw;
            }
        }

        public async Task<bool> DeleteTaskAsync(int taskId)
        {
            try
            {
                var task = await _context.DocumentDownloadTasks.FindAsync(taskId);
                if (task == null)
                    return false;

                _context.DocumentDownloadTasks.Remove(task);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted document download task: ID={TaskId}", taskId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task: {TaskId}", taskId);
                throw;
            }
        }

        public async Task<bool> RetryTaskAsync(int taskId)
        {
            try
            {
                var task = await _context.DocumentDownloadTasks.FindAsync(taskId);
                if (task == null)
                    return false;

                if (task.Status != Models.TaskStatus.Failed)
                {
                    throw new InvalidOperationException($"Task {taskId} is not in Failed status");
                }

                task.Status = Models.TaskStatus.Pending;
                task.RetryCount++;
                task.NextRetryAt = DateTime.UtcNow.AddMinutes(5); // Через 5 минут
                task.ErrorMessage = null;

                _context.DocumentDownloadTasks.Update(task);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Retried task: ID={TaskId}, RetryCount={RetryCount}", taskId, task.RetryCount);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying task: {TaskId}", taskId);
                throw;
            }
        }

        public async Task<bool> CancelTaskAsync(int taskId)
        {
            try
            {
                var task = await _context.DocumentDownloadTasks.FindAsync(taskId);
                if (task == null)
                    return false;

                if (task.Status == Models.TaskStatus.Completed || task.Status == Models.TaskStatus.Cancelled)
                {
                    throw new InvalidOperationException($"Task {taskId} is already completed or cancelled");
                }

                task.Status = Models.TaskStatus.Cancelled;
                task.CompletedAt = DateTime.UtcNow;

                _context.DocumentDownloadTasks.Update(task);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cancelled task: ID={TaskId}", taskId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling task: {TaskId}", taskId);
                throw;
            }
        }

        public async Task<DocumentDownloadTaskStatsDto> GetStatsAsync()
        {
            try
            {
                var tasks = await _context.DocumentDownloadTasks.ToListAsync();

                var stats = new DocumentDownloadTaskStatsDto
                {
                    TotalTasks = tasks.Count,
                    PendingTasks = tasks.Count(t => t.Status == Models.TaskStatus.Pending),
                    InProgressTasks = tasks.Count(t => t.Status == Models.TaskStatus.InProgress),
                    CompletedTasks = tasks.Count(t => t.Status == Models.TaskStatus.Completed),
                    FailedTasks = tasks.Count(t => t.Status == Models.TaskStatus.Failed),
                    CancelledTasks = tasks.Count(t => t.Status == Models.TaskStatus.Cancelled),
                    TasksWithErrors = tasks.Count(t => !string.IsNullOrEmpty(t.ErrorMessage))
                };

                // Рассчитываем среднее время выполнения
                var completedTasks = tasks.Where(t => t.Status == Models.TaskStatus.Completed && 
                                                     t.StartedAt.HasValue && t.CompletedAt.HasValue)
                                         .ToList();

                if (completedTasks.Any())
                {
                    var totalHours = completedTasks
                        .Sum(t => (t.CompletedAt.Value - t.StartedAt.Value).TotalHours);
                    stats.AverageCompletionTimeHours = totalHours / completedTasks.Count;
                }

                // Рассчитываем процент успешных задач
                var processedTasks = tasks.Where(t => t.Status == Models.TaskStatus.Completed || 
                                                     t.Status == Models.TaskStatus.Failed)
                                         .ToList();

                if (processedTasks.Any())
                {
                    var successfulTasks = processedTasks.Count(t => t.Status == Models.TaskStatus.Completed);
                    stats.SuccessRate = (double)successfulTasks / processedTasks.Count * 100;
                }

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task statistics");
                throw;
            }
        }

        public async Task<List<DocumentDownloadTaskDto>> GetPendingTasksAsync(int limit = 100)
        {
            try
            {
                var tasks = await _context.DocumentDownloadTasks
                    .Include(t => t.Tender)
                    .Where(t => t.Status == Models.TaskStatus.Pending)
                    .OrderByDescending(t => t.Priority)
                    .ThenBy(t => t.CreatedAt)
                    .Take(limit)
                    .ToListAsync();

                return tasks.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending tasks");
                throw;
            }
        }

        public async Task<bool> ProcessTaskAsync(int taskId)
        {
            try
            {
                var task = await _context.DocumentDownloadTasks
                    .Include(t => t.Tender)
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task == null || task.Status != Models.TaskStatus.Pending)
                    return false;

                // Обновляем статус задачи
                task.Status = Models.TaskStatus.InProgress;
                task.StartedAt = DateTime.UtcNow;
                _context.DocumentDownloadTasks.Update(task);
                await _context.SaveChangesAsync();

                try
                {
                    // Выполняем загрузку документа
                    List<TenderDocument> downloadedDocuments = new();

                    if (task.DocType == "all")
                    {
                        downloadedDocuments = await _documentService.DownloadAllDocumentsAsync(task.TenderId);
                    }
                    else if (task.DocType == "notification")
                    {
                        var document = await _documentService.GetNotificationDocumentAsync(task.TenderId);
                        if (document != null)
                            downloadedDocuments.Add(document);
                    }
                    else
                    {
                        var document = await _documentService.DownloadDocumentAsync(task.TenderId, task.DocType);
                        if (document != null)
                            downloadedDocuments.Add(document);
                    }

                    // Обновляем статус задачи
                    task.Status = Models.TaskStatus.Completed;
                    task.CompletedAt = DateTime.UtcNow;
                    _context.DocumentDownloadTasks.Update(task);

                    _logger.LogInformation("Processed task {TaskId}: downloaded {Count} documents", 
                        taskId, downloadedDocuments.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing task {TaskId}", taskId);

                    // Обновляем статус задачи с ошибкой
                    task.Status = Models.TaskStatus.Failed;
                    task.ErrorMessage = ex.Message;
                    task.RetryCount++;
                    
                    if (task.RetryCount < 3) // Макс. 3 попытки
                    {
                        task.NextRetryAt = DateTime.UtcNow.AddMinutes(5);
                        task.Status = Models.TaskStatus.Pending;
                    }

                    _context.DocumentDownloadTasks.Update(task);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessTaskAsync for task {TaskId}", taskId);
                throw;
            }
        }

        private DocumentDownloadTaskDto MapToDto(DocumentDownloadTask task)
        {
            return new DocumentDownloadTaskDto
            {
                Id = task.Id,
                TenderId = task.TenderId,
                TenderTitle = task.Tender?.Title,
                PurchaseNumber = task.Tender?.PurchaseNumber,
                DocType = task.DocType,
                CreatedAt = task.CreatedAt,
                StartedAt = task.StartedAt,
                CompletedAt = task.CompletedAt,
                Status = task.Status.ToString(),
                ErrorMessage = task.ErrorMessage,
                RetryCount = task.RetryCount,
                NextRetryAt = task.NextRetryAt,
                Priority = task.Priority,
                SettingsJson = task.SettingsJson
            };
        }
    }
}
