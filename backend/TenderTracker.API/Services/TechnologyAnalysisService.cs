using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.RegularExpressions;
using TenderTracker.API.Config;
using TenderTracker.API.Data;
using TenderTracker.API.Models;

namespace TenderTracker.API.Services
{
    public class TechnologyAnalysisService : ITechnologyAnalysisService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDocumentService _documentService;
        private readonly ILogger<TechnologyAnalysisService> _logger;
        private readonly TechnologyStackConfig _config;

        public TechnologyAnalysisService(
            ApplicationDbContext context,
            IDocumentService documentService,
            ILogger<TechnologyAnalysisService> logger,
            IOptions<TechnologyStackConfig> config)
        {
            _context = context;
            _documentService = documentService;
            _logger = logger;
            _config = config.Value;
        }

        public async Task<TechnologyAnalysis?> AnalyzeTenderAsync(int tenderId, int? documentId = null)
        {
            try
            {
                var tender = await _context.FoundTenders.FindAsync(tenderId);
                if (tender == null)
                {
                    _logger.LogWarning("Tender not found for analysis: {TenderId}", tenderId);
                    return null;
                }

                // Проверяем, есть ли уже анализ для этого тендера
                var existingAnalysis = await _context.TechnologyAnalyses
                    .FirstOrDefaultAsync(a => a.TenderId == tenderId && a.DocumentId == documentId);

                if (existingAnalysis != null && !existingAnalysis.ManuallyVerified)
                {
                    _logger.LogInformation("Analysis already exists for tender {TenderId}, updating...", tenderId);
                }

                // Получаем текст для анализа
                string textToAnalyze = await GetTextForAnalysisAsync(tenderId, documentId);
                if (string.IsNullOrEmpty(textToAnalyze))
                {
                    _logger.LogWarning("No text available for analysis for tender {TenderId}", tenderId);
                    return null;
                }

                // Анализируем текст
                var analysisResult = AnalyzeText(textToAnalyze);

                // Создаем или обновляем анализ
                var analysis = existingAnalysis ?? new TechnologyAnalysis
                {
                    TenderId = tenderId,
                    DocumentId = documentId,
                    AnalyzedAt = DateTime.UtcNow
                };

                analysis.MatchScore = analysisResult.MatchScore;
                analysis.IsCompatible = analysisResult.MatchScore >= _config.Settings.MinimumMatchScore;
                analysis.MatchedTechnologiesJson = JsonSerializer.Serialize(analysisResult.MatchedTechnologies);
                analysis.ManuallyVerified = false;

                if (existingAnalysis == null)
                {
                    _context.TechnologyAnalyses.Add(analysis);
                }
                else
                {
                    _context.TechnologyAnalyses.Update(analysis);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Analysis completed for tender {TenderId}: Score={Score}, Compatible={Compatible}",
                    tenderId, analysis.MatchScore, analysis.IsCompatible);

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing tender {TenderId}", tenderId);
                throw;
            }
        }

        public async Task<TechnologyAnalysis?> GetAnalysisAsync(int tenderId)
        {
            try
            {
                return await _context.TechnologyAnalyses
                    .Include(a => a.Tender)
                    .Include(a => a.Document)
                    .FirstOrDefaultAsync(a => a.TenderId == tenderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting analysis for tender {TenderId}", tenderId);
                throw;
            }
        }

        public async Task<TechnologyAnalysis?> GetAnalysisByIdAsync(int analysisId)
        {
            try
            {
                return await _context.TechnologyAnalyses
                    .Include(a => a.Tender)
                    .Include(a => a.Document)
                    .FirstOrDefaultAsync(a => a.Id == analysisId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting analysis by ID: {AnalysisId}", analysisId);
                throw;
            }
        }

        public async Task<List<TechnologyAnalysis>> GetAnalysesByCompatibilityAsync(bool isCompatible)
        {
            try
            {
                return await _context.TechnologyAnalyses
                    .Include(a => a.Tender)
                    .Where(a => a.IsCompatible == isCompatible)
                    .OrderByDescending(a => a.MatchScore)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting analyses by compatibility: {IsCompatible}", isCompatible);
                throw;
            }
        }

        public async Task<TechnologyAnalysis?> UpdateAnalysisAsync(int analysisId, int matchScore, bool isCompatible, string? notes = null)
        {
            try
            {
                var analysis = await _context.TechnologyAnalyses.FindAsync(analysisId);
                if (analysis == null)
                {
                    return null;
                }

                analysis.MatchScore = matchScore;
                analysis.IsCompatible = isCompatible;
                analysis.AnalysisNotes = notes;
                analysis.ManuallyVerified = true;
                analysis.AnalyzedAt = DateTime.UtcNow;

                _context.TechnologyAnalyses.Update(analysis);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Analysis updated: {AnalysisId}", analysisId);
                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating analysis: {AnalysisId}", analysisId);
                throw;
            }
        }

        public async Task<bool> MarkAsManuallyVerifiedAsync(int analysisId, bool verified = true, string? notes = null)
        {
            try
            {
                var analysis = await _context.TechnologyAnalyses.FindAsync(analysisId);
                if (analysis == null)
                {
                    return false;
                }

                analysis.ManuallyVerified = verified;
                analysis.AnalysisNotes = notes ?? analysis.AnalysisNotes;

                _context.TechnologyAnalyses.Update(analysis);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Analysis marked as manually verified: {AnalysisId}, Verified={Verified}",
                    analysisId, verified);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking analysis as verified: {AnalysisId}", analysisId);
                throw;
            }
        }

        public async Task<List<FoundTender>> GetCompatibleTendersAsync(int minMatchScore = 60)
        {
            try
            {
                return await _context.FoundTenders
                    .Include(t => t.TechnologyAnalyses)
                    .Where(t => t.TechnologyAnalyses.Any(a => 
                        a.IsCompatible && a.MatchScore >= minMatchScore && a.ManuallyVerified))
                    .OrderByDescending(t => t.TechnologyAnalyses.Max(a => a.MatchScore))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting compatible tenders with min score {MinScore}", minMatchScore);
                throw;
            }
        }

        public async Task<Dictionary<string, int>> GetTechnologyStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var query = _context.TechnologyAnalyses.AsQueryable();

                if (fromDate.HasValue)
                {
                    query = query.Where(a => a.AnalyzedAt >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(a => a.AnalyzedAt <= toDate.Value);
                }

                var analyses = await query.ToListAsync();
                var statistics = new Dictionary<string, int>();

                foreach (var analysis in analyses)
                {
                    try
                    {
                        var matchedTechs = JsonSerializer.Deserialize<List<MatchedTechnology>>(
                            analysis.MatchedTechnologiesJson) ?? new List<MatchedTechnology>();

                        foreach (var tech in matchedTechs)
                        {
                            if (statistics.ContainsKey(tech.Technology))
                            {
                                statistics[tech.Technology] += tech.Count;
                            }
                            else
                            {
                                statistics[tech.Technology] = tech.Count;
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // Пропускаем некорректные JSON
                        continue;
                    }
                }

                return statistics.OrderByDescending(kv => kv.Value).ToDictionary(kv => kv.Key, kv => kv.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting technology statistics");
                throw;
            }
        }

        private async Task<string> GetTextForAnalysisAsync(int tenderId, int? documentId)
        {
            try
            {
                string text = string.Empty;

                // Если указан конкретный документ, анализируем его
                if (documentId.HasValue)
                {
                    var document = await _documentService.GetDocumentByIdAsync(documentId.Value);
                    if (document != null)
                    {
                        text = ExtractTextFromJson(document.SourceJson);
                    }
                }
                else
                {
                    // Иначе получаем извещение (ТЗ)
                    var notification = await _documentService.GetNotificationDocumentAsync(tenderId);
                    if (notification != null)
                    {
                        text = ExtractTextFromJson(notification.SourceJson);
                    }
                    else
                    {
                        // Если нет извещения, используем заголовок и описание тендера
                        var tender = await _context.FoundTenders.FindAsync(tenderId);
                        if (tender != null)
                        {
                            text = $"{tender.Title} {tender.AdditionalInfo}";
                        }
                    }
                }

                // Ограничиваем длину текста
                if (text.Length > _config.Settings.MaxTextLength)
                {
                    text = text.Substring(0, _config.Settings.MaxTextLength);
                }

                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting text for analysis for tender {TenderId}", tenderId);
                return string.Empty;
            }
        }

        private string ExtractTextFromJson(string json)
        {
            try
            {
                var jsonDocument = JsonDocument.Parse(json);
                return ExtractTextFromJsonElement(jsonDocument.RootElement);
            }
            catch (JsonException)
            {
                // Если не удалось распарсить JSON, возвращаем как есть
                return json;
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
                        texts.Add(property.Name);
                        texts.Add(ExtractTextFromJsonElement(property.Value));
                    }
                    break;

                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        texts.Add(ExtractTextFromJsonElement(item));
                    }
                    break;

                case JsonValueKind.String:
                    texts.Add(element.GetString() ?? string.Empty);
                    break;

                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                    texts.Add(element.ToString());
                    break;
            }

            return string.Join(" ", texts.Where(t => !string.IsNullOrEmpty(t)));
        }

        private AnalysisResult AnalyzeText(string text)
        {
            var matchedTechnologies = new List<MatchedTechnology>();
            var normalizedText = text.ToLowerInvariant();
            int totalMatches = 0;

            foreach (var tech in _config.Technologies)
            {
                int count = 0;
                var mentions = new List<string>();

                // Ищем основное название технологии
                if (normalizedText.Contains(tech.Name.ToLowerInvariant()))
                {
                    count++;
                    mentions.Add(tech.Name);
                }

                // Ищем алиасы
                foreach (var alias in tech.Aliases)
                {
                    var aliasLower = alias.ToLowerInvariant();
                    if (normalizedText.Contains(aliasLower))
                    {
                        count++;
                        mentions.Add(alias);
                    }
                }

                if (count > 0)
                {
                    matchedTechnologies.Add(new MatchedTechnology
                    {
                        Technology = tech.Name,
                        Count = count * tech.Weight,
                        Mentions = mentions.Distinct().ToList()
                    });
                    totalMatches += count * tech.Weight;
                }
            }

            // Рассчитываем процент совпадения
            int maxPossibleMatches = _config.Technologies.Sum(t => t.Weight * 3); // Примерная максимальная оценка
            int matchScore = maxPossibleMatches > 0 ? (totalMatches * 100) / maxPossibleMatches : 0;

            // Ограничиваем 100%
            matchScore = Math.Min(matchScore, 100);

            return new AnalysisResult
            {
                MatchScore = matchScore,
                MatchedTechnologies = matchedTechnologies
            };
        }

        private class AnalysisResult
        {
            public int MatchScore { get; set; }
            public List<MatchedTechnology> MatchedTechnologies { get; set; } = new List<MatchedTechnology>();
        }

        private class MatchedTechnology
        {
            public string Technology { get; set; } = string.Empty;
            public int Count { get; set; }
            public List<string> Mentions { get; set; } = new List<string>();
        }
    }
}
