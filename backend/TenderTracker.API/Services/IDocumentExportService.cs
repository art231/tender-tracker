using TenderTracker.API.Models;

namespace TenderTracker.API.Services
{
    public interface IDocumentExportService
    {
        /// <summary>
        /// Экспортирует документ в формат PDF
        /// </summary>
        /// <param name="document">Документ для экспорта</param>
        /// <param name="outputPath">Путь для сохранения файла (опционально)</param>
        /// <returns>Путь к созданному файлу</returns>
        Task<string> ExportToPdfAsync(TenderDocument document, string? outputPath = null);

        /// <summary>
        /// Экспортирует документ в формат DOCX
        /// </summary>
        /// <param name="document">Документ для экспорта</param>
        /// <param name="outputPath">Путь для сохранения файла (опционально)</param>
        /// <returns>Путь к созданному файлу</returns>
        Task<string> ExportToDocxAsync(TenderDocument document, string? outputPath = null);

        /// <summary>
        /// Генерирует уникальное имя файла для экспорта
        /// </summary>
        /// <param name="document">Документ</param>
        /// <param name="format">Формат файла (pdf, docx)</param>
        /// <returns>Уникальное имя файла</returns>
        string GenerateExportFileName(TenderDocument document, string format);

        /// <summary>
        /// Получает путь к директории для экспорта документов
        /// </summary>
        /// <returns>Путь к директории экспорта</returns>
        string GetExportDirectoryPath();
    }
}
