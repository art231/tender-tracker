using TenderTracker.API.Models;

namespace TenderTracker.API.Services
{
    public interface IDocumentService
    {
        Task<TenderDocument?> DownloadDocumentAsync(int tenderId, string docType);
        Task<List<TenderDocument>> DownloadAllDocumentsAsync(int tenderId);
        Task<TenderDocument?> GetNotificationDocumentAsync(int tenderId);
        Task<List<TenderDocument>> GetTenderDocumentsAsync(int tenderId);
        Task<TenderDocument?> GetDocumentByIdAsync(int documentId);
        Task<bool> DeleteDocumentAsync(int documentId);
        Task<string?> ExportDocumentToPdfAsync(int documentId);
        Task<string?> ExportDocumentToDocxAsync(int documentId);
    }
}
