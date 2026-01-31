using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.RegularExpressions;
using TenderTracker.API.Config;
using TenderTracker.API.Models;
using iTextSharpText = iTextSharp.text;
using iTextSharpPdf = iTextSharp.text.pdf;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace TenderTracker.API.Services
{
    public class DocumentExportService : IDocumentExportService
    {
        private readonly ILogger<DocumentExportService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly ExportSettings _settings;

        public DocumentExportService(
            ILogger<DocumentExportService> logger,
            IWebHostEnvironment environment,
            IOptions<ExportSettings> settings)
        {
            _logger = logger;
            _environment = environment;
            _settings = settings.Value;
        }

        public async Task<string> ExportToPdfAsync(TenderDocument document, string? outputPath = null)
        {
            try
            {
                _logger.LogInformation("Exporting document {DocumentId} to PDF", document.Id);

                // Генерируем путь для сохранения файла
                var fileName = GenerateExportFileName(document, "pdf");
                var filePath = outputPath ?? Path.Combine(GetExportDirectoryPath(), fileName);

                // Создаем директорию, если она не существует
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Извлекаем текст из JSON документа
                var documentText = ExtractTextFromDocument(document);

                // Создаем PDF документ
                using (var fs = new FileStream(filePath, FileMode.Create))
                {
                    var documentPdf = new iTextSharpText.Document(iTextSharpText.PageSize.A4, 50, 50, 50, 50);
                    var writer = iTextSharpPdf.PdfWriter.GetInstance(documentPdf, fs);

                    documentPdf.Open();

                    // Добавляем метаданные
                    documentPdf.AddTitle($"Документ тендера: {document.DocType}");
                    documentPdf.AddSubject($"Тендер #{document.TenderId}");
                    documentPdf.AddKeywords($"тендер, документ, {document.DocType}");
                    documentPdf.AddCreator("TenderTracker");
                    documentPdf.AddAuthor("TenderTracker System");

                    // Добавляем заголовок
                    var titleFont = iTextSharpText.FontFactory.GetFont("Arial", 16, iTextSharpText.Font.BOLD);
                    var title = new iTextSharpText.Paragraph($"Документ тендера: {document.DocType}", titleFont)
                    {
                        Alignment = iTextSharpText.Element.ALIGN_CENTER,
                        SpacingAfter = 20
                    };
                    documentPdf.Add(title);

                    // Добавляем информацию о документе
                    var infoFont = iTextSharpText.FontFactory.GetFont("Arial", 10);
                    var infoTable = new iTextSharpPdf.PdfPTable(2)
                    {
                        WidthPercentage = 100,
                        SpacingBefore = 10,
                        SpacingAfter = 20
                    };

                    AddInfoRow(infoTable, "ID документа:", document.Id.ToString(), infoFont);
                    AddInfoRow(infoTable, "ID тендера:", document.TenderId.ToString(), infoFont);
                    AddInfoRow(infoTable, "Тип документа:", document.DocType, infoFont);
                    
                    if (document.PublishedAt.HasValue)
                    {
                        AddInfoRow(infoTable, "Дата публикации:", 
                            document.PublishedAt.Value.ToString("dd.MM.yyyy HH:mm"), infoFont);
                    }
                    
                    AddInfoRow(infoTable, "Дата скачивания:", 
                        document.DownloadedAt.ToString("dd.MM.yyyy HH:mm"), infoFont);

                    documentPdf.Add(infoTable);

                    // Добавляем разделитель
                    documentPdf.Add(new iTextSharpText.Chunk("\n"));

                    // Добавляем содержимое документа
                    var contentFont = iTextSharpText.FontFactory.GetFont("Arial", 11);
                    var content = new iTextSharpText.Paragraph(documentText, contentFont)
                    {
                        Alignment = iTextSharpText.Element.ALIGN_JUSTIFIED,
                        SpacingBefore = 10
                    };
                    documentPdf.Add(content);

                    // Добавляем футер с информацией о системе
                    documentPdf.Add(new iTextSharpText.Chunk("\n\n"));
                    var footerFont = iTextSharpText.FontFactory.GetFont("Arial", 8, iTextSharpText.Font.ITALIC);
                    var footer = new iTextSharpText.Paragraph(
                        $"Экспортировано системой TenderTracker {DateTime.Now:dd.MM.yyyy HH:mm}", 
                        footerFont)
                    {
                        Alignment = iTextSharpText.Element.ALIGN_RIGHT
                    };
                    documentPdf.Add(footer);

                    documentPdf.Close();
                }

                _logger.LogInformation("PDF exported successfully: {FilePath}", filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting document {DocumentId} to PDF", document.Id);
                throw;
            }
        }

        public async Task<string> ExportToDocxAsync(TenderDocument document, string? outputPath = null)
        {
            try
            {
                _logger.LogInformation("Exporting document {DocumentId} to DOCX", document.Id);

                // Генерируем путь для сохранения файла
                var fileName = GenerateExportFileName(document, "docx");
                var filePath = outputPath ?? Path.Combine(GetExportDirectoryPath(), fileName);

                // Создаем директорию, если она не существует
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Извлекаем текст из JSON документа
                var documentText = ExtractTextFromDocument(document);

                // Создаем DOCX документ
                using (var wordDocument = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
                {
                    // Добавляем основную часть документа
                    var mainPart = wordDocument.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    var body = mainPart.Document.AppendChild(new Body());

                    // Добавляем заголовок
                    var titleParagraph = new Paragraph();
                    var titleRun = new Run();
                    var titleText = new Text($"Документ тендера: {document.DocType}");
                    titleRun.Append(new RunProperties(
                        new Bold(),
                        new FontSize { Val = "32" },
                        new RunFonts { Ascii = "Arial" }
                    ));
                    titleRun.Append(titleText);
                    titleParagraph.Append(new ParagraphProperties(
                        new Justification { Val = JustificationValues.Center }
                    ));
                    titleParagraph.Append(titleRun);
                    body.Append(titleParagraph);

                    // Добавляем пустую строку
                    body.Append(new Paragraph(new Run(new Text(""))));

                    // Добавляем таблицу с информацией
                    var infoTable = new Table();
                    
                    // Настройки таблицы
                    var tableProperties = new TableProperties(
                        new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }
                    );
                    infoTable.AppendChild(tableProperties);

                    // Добавляем строки с информацией
                    AddDocxTableRow(infoTable, "ID документа:", document.Id.ToString());
                    AddDocxTableRow(infoTable, "ID тендера:", document.TenderId.ToString());
                    AddDocxTableRow(infoTable, "Тип документа:", document.DocType);
                    
                    if (document.PublishedAt.HasValue)
                    {
                        AddDocxTableRow(infoTable, "Дата публикации:", 
                            document.PublishedAt.Value.ToString("dd.MM.yyyy HH:mm"));
                    }
                    
                    AddDocxTableRow(infoTable, "Дата скачивания:", 
                        document.DownloadedAt.ToString("dd.MM.yyyy HH:mm"));

                    body.Append(infoTable);

                    // Добавляем пустую строку
                    body.Append(new Paragraph(new Run(new Text(""))));

                    // Добавляем содержимое документа
                    var contentParagraph = new Paragraph();
                    var contentRun = new Run();
                    var contentText = new Text(documentText);
                    contentRun.Append(new RunProperties(
                        new FontSize { Val = "22" },
                        new RunFonts { Ascii = "Arial" }
                    ));
                    contentRun.Append(contentText);
                    contentParagraph.Append(contentRun);
                    body.Append(contentParagraph);

                    // Добавляем футер
                    body.Append(new Paragraph(new Run(new Text(""))));
                    body.Append(new Paragraph(new Run(new Text(""))));
                    
                    var footerParagraph = new Paragraph();
                    var footerRun = new Run();
                    var footerText = new Text(
                        $"Экспортировано системой TenderTracker {DateTime.Now:dd.MM.yyyy HH:mm}");
                    footerRun.Append(new RunProperties(
                        new Italic(),
                        new FontSize { Val = "16" },
                        new RunFonts { Ascii = "Arial" }
                    ));
                    footerRun.Append(footerText);
                    footerParagraph.Append(new ParagraphProperties(
                        new Justification { Val = JustificationValues.Right }
                    ));
                    footerParagraph.Append(footerRun);
                    body.Append(footerParagraph);

                    mainPart.Document.Save();
                }

                _logger.LogInformation("DOCX exported successfully: {FilePath}", filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting document {DocumentId} to DOCX", document.Id);
                throw;
            }
        }

        public string GenerateExportFileName(TenderDocument document, string format)
        {
            var safeDocType = Regex.Replace(document.DocType, @"[^\w\-]", "_");
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"tender_{document.TenderId}_{safeDocType}_{timestamp}.{format.ToLower()}";
        }

        public string GetExportDirectoryPath()
        {
            var exportPath = Path.Combine(_environment.ContentRootPath, "Exports", "Documents");
            
            // Создаем директорию, если она не существует
            if (!Directory.Exists(exportPath))
            {
                Directory.CreateDirectory(exportPath);
            }

            return exportPath;
        }

        private string ExtractTextFromDocument(TenderDocument document)
        {
            try
            {
                if (string.IsNullOrEmpty(document.SourceJson))
                {
                    return "Содержимое документа отсутствует.";
                }

                var jsonDocument = JsonDocument.Parse(document.SourceJson);
                return ExtractTextFromJsonElement(jsonDocument.RootElement);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse JSON for document {DocumentId}", document.Id);
                return document.SourceJson;
            }
        }

        private string ExtractTextFromJsonElement(JsonElement element)
        {
            var texts = new List<string>();

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        // Пропускаем технические поля
                        if (!IsTechnicalField(property.Name))
                        {
                            texts.Add(property.Name);
                            texts.Add(ExtractTextFromJsonElement(property.Value));
                        }
                    }
                    break;

                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        texts.Add(ExtractTextFromJsonElement(item));
                    }
                    break;

                case JsonValueKind.String:
                    var stringValue = element.GetString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(stringValue))
                    {
                        texts.Add(stringValue);
                    }
                    break;

                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                    texts.Add(element.ToString());
                    break;
            }

            return string.Join(" ", texts.Where(t => !string.IsNullOrEmpty(t)));
        }

        private bool IsTechnicalField(string fieldName)
        {
            var technicalFields = new[]
            {
                "id", "_id", "guid", "uuid", "created_at", "updated_at", 
                "version", "hash", "signature", "metadata"
            };

            return technicalFields.Contains(fieldName.ToLowerInvariant());
        }

        private void AddInfoRow(iTextSharpPdf.PdfPTable table, string label, string value, iTextSharpText.Font font)
        {
            var labelCell = new iTextSharpPdf.PdfPCell(new iTextSharpText.Phrase(label, font))
            {
                BorderWidth = 0,
                PaddingBottom = 5,
                HorizontalAlignment = iTextSharpText.Element.ALIGN_LEFT
            };

            var valueCell = new iTextSharpPdf.PdfPCell(new iTextSharpText.Phrase(value, font))
            {
                BorderWidth = 0,
                PaddingBottom = 5,
                HorizontalAlignment = iTextSharpText.Element.ALIGN_LEFT
            };

            table.AddCell(labelCell);
            table.AddCell(valueCell);
        }

        private void AddDocxTableRow(Table table, string label, string value)
        {
            var row = new TableRow();

            // Ячейка с меткой
            var labelCell = new TableCell();
            labelCell.Append(new Paragraph(new Run(new Text(label))
            {
                RunProperties = new RunProperties(
                    new Bold(),
                    new FontSize { Val = "20" }
                )
            }));
            row.Append(labelCell);

            // Ячейка со значением
            var valueCell = new TableCell();
            valueCell.Append(new Paragraph(new Run(new Text(value))
            {
                RunProperties = new RunProperties(
                    new FontSize { Val = "20" }
                )
            }));
            row.Append(valueCell);

            table.Append(row);
        }
    }

    public class ExportSettings
    {
        public string PdfTemplatePath { get; set; } = "Templates/pdf-template.html";
        public string DocxTemplatePath { get; set; } = "Templates/docx-template.xml";
        public int CacheDurationHours { get; set; } = 24;
        public string ExportDirectory { get; set; } = "Exports/Documents";
    }
}
