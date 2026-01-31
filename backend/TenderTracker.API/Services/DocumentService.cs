using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TenderTracker.API.Clients;
using TenderTracker.API.Data;
using TenderTracker.API.Models;

namespace TenderTracker.API.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IGosPlanApiClient _apiClient;
        private readonly ILogger<DocumentService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IDocumentExportService _exportService;

        public DocumentService(
            ApplicationDbContext context,
            IGosPlanApiClient apiClient,
            ILogger<DocumentService> logger,
            IWebHostEnvironment environment,
            IDocumentExportService exportService)
        {
            _context = context;
            _apiClient = apiClient;
            _logger = logger;
            _environment = environment;
            _exportService = exportService;
        }

        public async Task<TenderDocument?> DownloadDocumentAsync(int tenderId, string docType)
        {
            try
            {
                var tender = await _context.FoundTenders.FindAsync(tenderId);
                if (tender == null)
                {
                    _logger.LogWarning("Tender not found: {TenderId}", tenderId);
                    return null;
                }

                // Проверяем, не скачан ли уже документ этого типа
                var existingDocument = await _context.TenderDocuments
                    .FirstOrDefaultAsync(d => d.TenderId == tenderId && d.DocType == docType);

                if (existingDocument != null)
                {
                    _logger.LogInformation("Document already downloaded: {DocType} for tender {TenderId}", docType, tenderId);
                    return existingDocument;
                }

                // Получаем документ из API
                var document = await _apiClient.GetDocumentByTypeAsync(tender.PurchaseNumber, docType);
                if (document == null)
                {
                    _logger.LogWarning("Document not found in API: {DocType} for tender {PurchaseNumber}", 
                        docType, tender.PurchaseNumber);
                    return null;
                }

                // Сохраняем документ в БД
                var tenderDocument = new TenderDocument
                {
                    TenderId = tenderId,
                    DocType = docType,
                    PublishedAt = document.PublishedAt,
                    DownloadedAt = DateTime.UtcNow,
                    SourceJson = document.Source?.ToString() ?? "{}"
                };

                _context.TenderDocuments.Add(tenderDocument);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Document downloaded successfully: {DocType} for tender {TenderId}", 
                    docType, tenderId);

                return tenderDocument;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document: {DocType} for tender {TenderId}", 
                    docType, tenderId);
                throw;
            }
        }

        public async Task<List<TenderDocument>> DownloadAllDocumentsAsync(int tenderId)
        {
            try
            {
                var tender = await _context.FoundTenders.FindAsync(tenderId);
                if (tender == null)
                {
                    _logger.LogWarning("Tender not found: {TenderId}", tenderId);
                    return new List<TenderDocument>();
                }

                // Получаем все документы из API
                var documents = await _apiClient.GetAllDocumentsAsync(tender.PurchaseNumber);
                if (documents == null || !documents.Any())
                {
                    _logger.LogWarning("No documents found in API for tender {PurchaseNumber}", 
                        tender.PurchaseNumber);
                    return new List<TenderDocument>();
                }

                var downloadedDocuments = new List<TenderDocument>();

                foreach (var document in documents)
                {
                    // Проверяем, не скачан ли уже документ этого типа
                    var existingDocument = await _context.TenderDocuments
                        .FirstOrDefaultAsync(d => d.TenderId == tenderId && d.DocType == document.DocType);

                    if (existingDocument != null)
                    {
                        downloadedDocuments.Add(existingDocument);
                        continue;
                    }

                    // Сохраняем новый документ
                    var tenderDocument = new TenderDocument
                    {
                        TenderId = tenderId,
                        DocType = document.DocType ?? "unknown",
                        PublishedAt = document.PublishedAt,
                        DownloadedAt = DateTime.UtcNow,
                        SourceJson = document.Source?.ToString() ?? "{}"
                    };

                    _context.TenderDocuments.Add(tenderDocument);
                    downloadedDocuments.Add(tenderDocument);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Downloaded {Count} documents for tender {TenderId}", 
                    downloadedDocuments.Count, tenderId);

                return downloadedDocuments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading all documents for tender {TenderId}", tenderId);
                throw;
            }
        }

        public async Task<TenderDocument?> GetNotificationDocumentAsync(int tenderId)
        {
            try
            {
                // Сначала пытаемся найти уже скачанный документ
                var existingDocument = await _context.TenderDocuments
                    .FirstOrDefaultAsync(d => d.TenderId == tenderId && 
                        (d.DocType.Contains("notification", StringComparison.OrdinalIgnoreCase) ||
                         d.DocType.Contains("notice", StringComparison.OrdinalIgnoreCase)));

                if (existingDocument != null)
                {
                    return existingDocument;
                }

                // Если не нашли, скачиваем
                return await DownloadDocumentAsync(tenderId, "notification");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification document for tender {TenderId}", tenderId);
                throw;
            }
        }

        public async Task<List<TenderDocument>> GetTenderDocumentsAsync(int tenderId)
        {
            try
            {
                return await _context.TenderDocuments
                    .Where(d => d.TenderId == tenderId)
                    .OrderByDescending(d => d.PublishedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents for tender {TenderId}", tenderId);
                throw;
            }
        }

        public async Task<TenderDocument?> GetDocumentByIdAsync(int documentId)
        {
            try
            {
                return await _context.TenderDocuments
                    .Include(d => d.Tender)
                    .FirstOrDefaultAsync(d => d.Id == documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document by ID: {DocumentId}", documentId);
                throw;
            }
        }

        public async Task<bool> DeleteDocumentAsync(int documentId)
        {
            try
            {
                var document = await _context.TenderDocuments.FindAsync(documentId);
                if (document == null)
                {
                    return false;
                }

                // Удаляем связанный файл, если он существует
                if (!string.IsNullOrEmpty(document.FilePath) && File.Exists(document.FilePath))
                {
                    File.Delete(document.FilePath);
                }

                _context.TenderDocuments.Remove(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Document deleted: {DocumentId}", documentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document: {DocumentId}", documentId);
                throw;
            }
        }

        public async Task<string?> ExportDocumentToPdfAsync(int documentId)
        {
            try
            {
                _logger.LogInformation("Exporting document {DocumentId} to PDF", documentId);

                // Получаем документ из БД
                var document = await GetDocumentByIdAsync(documentId);
                if (document == null)
                {
                    _logger.LogWarning("Document not found: {DocumentId}", documentId);
                    return null;
                }

                // Экспортируем в PDF
                var filePath = await _exportService.ExportToPdfAsync(document);

                // Обновляем путь к файлу в документе
                document.FilePath = filePath;
                _context.TenderDocuments.Update(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation("PDF export completed for document {DocumentId}: {FilePath}", 
                    documentId, filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting document {DocumentId} to PDF", documentId);
                throw;
            }
        }

        public async Task<string?> ExportDocumentToDocxAsync(int documentId)
        {
            try
            {
                _logger.LogInformation("Exporting document {DocumentId} to DOCX", documentId);

                // Получаем документ из БД
                var document = await GetDocumentByIdAsync(documentId);
                if (document == null)
                {
                    _logger.LogWarning("Document not found: {DocumentId}", documentId);
                    return null;
                }

                // Экспортируем в DOCX
                var filePath = await _exportService.ExportToDocxAsync(document);

                // Обновляем путь к файлу в документе
                document.FilePath = filePath;
                _context.TenderDocuments.Update(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation("DOCX export completed for document {DocumentId}: {FilePath}", 
                    documentId, filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting document {DocumentId} to DOCX", documentId);
                throw;
            }
        }
    }
}
