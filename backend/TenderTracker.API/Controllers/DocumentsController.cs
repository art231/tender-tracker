using Microsoft.AspNetCore.Mvc;
using TenderTracker.API.DTOs;
using TenderTracker.API.Services;
using System.Text.Json;

namespace TenderTracker.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(
            IDocumentService documentService,
            ILogger<DocumentsController> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }

        [HttpGet("tender/{tenderId}")]
        public async Task<ActionResult<List<TenderDocumentDto>>> GetTenderDocuments(int tenderId)
        {
            try
            {
                var documents = await _documentService.GetTenderDocumentsAsync(tenderId);
                var dtos = documents.Select(d => MapToDto(d)).ToList();
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents for tender {TenderId}", tenderId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TenderDocumentDto>> GetDocument(int id)
        {
            try
            {
                var document = await _documentService.GetDocumentByIdAsync(id);
                if (document == null)
                {
                    return NotFound();
                }

                return Ok(MapToDto(document));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document {DocumentId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("tender/{tenderId}/download")]
        public async Task<ActionResult<TenderDocumentDto>> DownloadDocument(int tenderId, [FromBody] DownloadDocumentRequest request)
        {
            try
            {
                var document = await _documentService.DownloadDocumentAsync(tenderId, request.DocType);
                if (document == null)
                {
                    return NotFound($"Document of type '{request.DocType}' not found for tender {tenderId}");
                }

                return Ok(MapToDto(document));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document for tender {TenderId}, type: {DocType}", 
                    tenderId, request.DocType);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("tender/{tenderId}/download-all")]
        public async Task<ActionResult<List<TenderDocumentDto>>> DownloadAllDocuments(int tenderId)
        {
            try
            {
                var documents = await _documentService.DownloadAllDocumentsAsync(tenderId);
                var dtos = documents.Select(d => MapToDto(d)).ToList();
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading all documents for tender {TenderId}", tenderId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("tender/{tenderId}/notification")]
        public async Task<ActionResult<TenderDocumentDto>> GetNotificationDocument(int tenderId)
        {
            try
            {
                var document = await _documentService.GetNotificationDocumentAsync(tenderId);
                if (document == null)
                {
                    return NotFound($"Notification document not found for tender {tenderId}");
                }

                return Ok(MapToDto(document));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification document for tender {TenderId}", tenderId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("{id}/export")]
        public async Task<ActionResult> ExportDocument(int id, [FromBody] ExportDocumentRequest request)
        {
            try
            {
                string? filePath = null;

                switch (request.Format.ToLower())
                {
                    case "pdf":
                        filePath = await _documentService.ExportDocumentToPdfAsync(id);
                        break;
                    case "docx":
                        filePath = await _documentService.ExportDocumentToDocxAsync(id);
                        break;
                    default:
                        return BadRequest("Unsupported format. Use 'pdf' or 'docx'.");
                }

                if (string.IsNullOrEmpty(filePath))
                {
                    return StatusCode(501, "Export not implemented for this format");
                }

                var fileName = Path.GetFileName(filePath);
                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                return File(fileBytes, GetContentType(request.Format), fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting document {DocumentId} to {Format}", id, request.Format);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteDocument(int id)
        {
            try
            {
                var result = await _documentService.DeleteDocumentAsync(id);
                if (!result)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        private TenderDocumentDto MapToDto(Models.TenderDocument document)
        {
            JsonElement? sourceJson = null;
            try
            {
                if (!string.IsNullOrEmpty(document.SourceJson))
                {
                    sourceJson = JsonSerializer.Deserialize<JsonElement>(document.SourceJson);
                }
            }
            catch (JsonException)
            {
                // Если не удалось распарсить JSON, оставляем null
            }

            return new TenderDocumentDto
            {
                Id = document.Id,
                TenderId = document.TenderId,
                DocType = document.DocType,
                PublishedAt = document.PublishedAt,
                DownloadedAt = document.DownloadedAt,
                SourceJson = sourceJson,
                FilePath = document.FilePath,
                TechnologyAnalysisId = document.TechnologyAnalysis?.Id
            };
        }

        private string GetContentType(string format)
        {
            return format.ToLower() switch
            {
                "pdf" => "application/pdf",
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };
        }
    }
}
