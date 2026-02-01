using TenderTracker.API.DTOs;
using TenderTracker.API.Models;

namespace TenderTracker.API.Services
{
    public interface ITenderExportService
    {
        /// <summary>
        /// Экспортирует тендеры в CSV формат
        /// </summary>
        /// <param name="tenders">Список тендеров для экспорта</param>
        /// <param name="includeAllFields">Включать все поля (true) или только основные (false)</param>
        /// <returns>CSV данные в виде строки</returns>
        Task<string> ExportToCsvAsync(IEnumerable<FoundTender> tenders, bool includeAllFields = true);

        /// <summary>
        /// Экспортирует тендеры в Excel формат (XLSX)
        /// </summary>
        /// <param name="tenders">Список тендеров для экспорта</param>
        /// <param name="includeAllFields">Включать все поля (true) или только основные (false)</param>
        /// <returns>Байтовый массив Excel файла</returns>
        Task<byte[]> ExportToExcelAsync(IEnumerable<FoundTender> tenders, bool includeAllFields = true);

        /// <summary>
        /// Экспортирует тендеры по параметрам поиска
        /// </summary>
        /// <param name="searchParams">Параметры поиска тендеров</param>
        /// <param name="format">Формат экспорта (csv, excel)</param>
        /// <param name="includeAllFields">Включать все поля</param>
        /// <returns>Результат экспорта (файл в виде потока)</returns>
        Task<ExportResult> ExportTendersAsync(TenderSearchParams searchParams, string format = "csv", bool includeAllFields = true);
    }

    public class ExportResult
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = "application/octet-stream";
        public string FileName { get; set; } = "export";
    }
}
