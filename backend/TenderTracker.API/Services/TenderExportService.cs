using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using OfficeOpenXml;
using TenderTracker.API.Data;
using TenderTracker.API.DTOs;
using TenderTracker.API.Models;
using Microsoft.EntityFrameworkCore;

namespace TenderTracker.API.Services
{
    public class TenderExportService : ITenderExportService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFoundTenderService _foundTenderService;
        private readonly ILogger<TenderExportService> _logger;

        public TenderExportService(
            ApplicationDbContext context,
            IFoundTenderService foundTenderService,
            ILogger<TenderExportService> logger)
        {
            _context = context;
            _foundTenderService = foundTenderService;
            _logger = logger;
        }

        public async Task<string> ExportToCsvAsync(IEnumerable<FoundTender> tenders, bool includeAllFields = true)
        {
            using var writer = new StringWriter();
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                Encoding = Encoding.UTF8
            });

            // Записываем заголовки
            if (includeAllFields)
            {
                csv.WriteHeader<CsvTenderFull>();
            }
            else
            {
                csv.WriteHeader<CsvTenderBasic>();
            }
            csv.NextRecord();

            // Записываем данные
            foreach (var tender in tenders)
            {
                if (includeAllFields)
                {
                    csv.WriteRecord(new CsvTenderFull(tender));
                }
                else
                {
                    csv.WriteRecord(new CsvTenderBasic(tender));
                }
                csv.NextRecord();
            }

            return writer.ToString();
        }

        public async Task<byte[]> ExportToExcelAsync(IEnumerable<FoundTender> tenders, bool includeAllFields = true)
        {
            // Установка лицензии EPPlus (для некоммерческого использования)
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Тендеры");

            // Заголовки
            var headers = includeAllFields 
                ? CsvTenderFull.GetHeaders() 
                : CsvTenderBasic.GetHeaders();
            
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            }

            // Данные
            int row = 2;
            foreach (var tender in tenders)
            {
                var values = includeAllFields 
                    ? CsvTenderFull.GetValues(tender) 
                    : CsvTenderBasic.GetValues(tender);
                
                for (int col = 0; col < values.Length; col++)
                {
                    worksheet.Cells[row, col + 1].Value = values[col];
                }
                row++;
            }

            // Автоширина колонок
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            return await package.GetAsByteArrayAsync();
        }

        public async Task<ExportResult> ExportTendersAsync(TenderSearchParams searchParams, string format = "csv", bool includeAllFields = true)
        {
            // Получаем тендеры по параметрам поиска
            var response = await _foundTenderService.GetTendersAsync(searchParams);
            var tenders = await _context.FoundTenders
                .Include(t => t.FoundByQuery)
                .Where(t => response.Tenders.Select(x => x.Id).Contains(t.Id))
                .ToListAsync();

            if (!tenders.Any())
            {
                throw new InvalidOperationException("Нет данных для экспорта");
            }

            byte[] content;
            string contentType;
            string fileName = $"tenders_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}";

            if (format.Equals("excel", StringComparison.OrdinalIgnoreCase))
            {
                content = await ExportToExcelAsync(tenders, includeAllFields);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName += ".xlsx";
            }
            else
            {
                var csvContent = await ExportToCsvAsync(tenders, includeAllFields);
                content = Encoding.UTF8.GetBytes(csvContent);
                contentType = "text/csv";
                fileName += ".csv";
            }

            return new ExportResult
            {
                Content = content,
                ContentType = contentType,
                FileName = fileName
            };
        }

        // Вспомогательные классы для CSV экспорта
        private class CsvTenderBasic
        {
            public string PurchaseNumber { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string? CustomerName { get; set; }
            public DateTime? PublishDate { get; set; }
            public string? DirectLinkToSource { get; set; }
            public DateTime? ApplicationDeadline { get; set; }
            public decimal? MaxPrice { get; set; }
            public string? Region { get; set; }

            public CsvTenderBasic() { }

            public CsvTenderBasic(FoundTender tender)
            {
                PurchaseNumber = tender.PurchaseNumber;
                Title = tender.Title;
                CustomerName = tender.CustomerName;
                PublishDate = tender.PublishDate;
                DirectLinkToSource = tender.DirectLinkToSource;
                ApplicationDeadline = tender.ApplicationDeadline;
                MaxPrice = tender.MaxPrice;
                Region = tender.Region;
            }

            public static string[] GetHeaders() => new[]
            {
                "Номер закупки",
                "Название",
                "Заказчик",
                "Дата публикации",
                "Ссылка на источник",
                "Срок подачи заявок",
                "Максимальная цена",
                "Регион"
            };

            public static string[] GetValues(FoundTender tender) => new[]
            {
                tender.PurchaseNumber,
                tender.Title,
                tender.CustomerName ?? "",
                tender.PublishDate?.ToString("yyyy-MM-dd HH:mm") ?? "",
                tender.DirectLinkToSource ?? "",
                tender.ApplicationDeadline?.ToString("yyyy-MM-dd HH:mm") ?? "",
                tender.MaxPrice?.ToString(CultureInfo.InvariantCulture) ?? "",
                tender.Region ?? ""
            };
        }

        private class CsvTenderFull : CsvTenderBasic
        {
            public string? CustomerInn { get; set; }
            public string? AdditionalInfo { get; set; }
            public string? PlanNumbers { get; set; }
            public string? FoundByQueryKeyword { get; set; }
            public DateTime SavedAt { get; set; }

            public CsvTenderFull() { }

            public CsvTenderFull(FoundTender tender) : base(tender)
            {
                CustomerInn = tender.CustomerInn;
                AdditionalInfo = tender.AdditionalInfo;
                PlanNumbers = !string.IsNullOrEmpty(tender.PlanNumbersJson) 
                    ? string.Join(", ", System.Text.Json.JsonSerializer.Deserialize<List<string>>(tender.PlanNumbersJson) ?? new List<string>())
                    : null;
                FoundByQueryKeyword = tender.FoundByQuery?.Keyword;
                SavedAt = tender.SavedAt;
            }

            public new static string[] GetHeaders() => new[]
            {
                "Номер закупки",
                "Название",
                "Заказчик",
                "Дата публикации",
                "Ссылка на источник",
                "Срок подачи заявок",
                "Максимальная цена",
                "Регион",
                "ИНН заказчика",
                "Дополнительная информация",
                "Планы-графики",
                "Ключевое слово поиска",
                "Дата сохранения"
            };

            public new static string[] GetValues(FoundTender tender)
            {
                var basicValues = CsvTenderBasic.GetValues(tender);
                var fullValues = new List<string>(basicValues)
                {
                    tender.CustomerInn ?? "",
                    tender.AdditionalInfo ?? "",
                    !string.IsNullOrEmpty(tender.PlanNumbersJson) 
                        ? string.Join(", ", System.Text.Json.JsonSerializer.Deserialize<List<string>>(tender.PlanNumbersJson) ?? new List<string>())
                        : "",
                    tender.FoundByQuery?.Keyword ?? "",
                    tender.SavedAt.ToString("yyyy-MM-dd HH:mm")
                };
                return fullValues.ToArray();
            }
        }
    }
}
