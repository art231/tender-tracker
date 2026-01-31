using TenderTracker.API.Models;

namespace TenderTracker.API.Services
{
    public interface ITechnologyAnalysisService
    {
        Task<TechnologyAnalysis?> AnalyzeTenderAsync(int tenderId, int? documentId = null);
        Task<TechnologyAnalysis?> GetAnalysisAsync(int tenderId);
        Task<TechnologyAnalysis?> GetAnalysisByIdAsync(int analysisId);
        Task<List<TechnologyAnalysis>> GetAnalysesByCompatibilityAsync(bool isCompatible);
        Task<TechnologyAnalysis?> UpdateAnalysisAsync(int analysisId, int matchScore, bool isCompatible, string? notes = null);
        Task<bool> MarkAsManuallyVerifiedAsync(int analysisId, bool verified = true, string? notes = null);
        Task<List<FoundTender>> GetCompatibleTendersAsync(int minMatchScore = 60);
        Task<Dictionary<string, int>> GetTechnologyStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    }
}
