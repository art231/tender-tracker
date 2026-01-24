using System.Text.Json;
using Microsoft.Extensions.Options;
using TenderTracker.API.Models;

namespace TenderTracker.API.Clients
{
    public class GosPlanApiOptions
    {
        public string BaseUrl { get; set; } = "https://v2.gosplan.info/api/v2";
        public int RequestDelayMs { get; set; } = 1000; // 1 запрос в секунду
        public int MaxRetries { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 30;
    }

    public interface IGosPlanApiClient
    {
        Task<List<GosPlanTender>> SearchTendersAsync(string keyword, int? queryId = null);
    }

    public class GosPlanApiClient : IGosPlanApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GosPlanApiClient> _logger;
        private readonly GosPlanApiOptions _options;
        private static readonly SemaphoreSlim _rateLimiter = new SemaphoreSlim(1, 1);
        private DateTime _lastRequestTime = DateTime.MinValue;

        public GosPlanApiClient(
            HttpClient httpClient, 
            IOptions<GosPlanApiOptions> options,
            ILogger<GosPlanApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _options = options.Value;

            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "TenderTracker/1.0");
        }

        public async Task<List<GosPlanTender>> SearchTendersAsync(string keyword, int? queryId = null)
        {
            await ApplyRateLimit();

            try
            {
                var endpoint = $"/fz44/purchases?search_description={Uri.EscapeDataString(keyword)}&limit=50";
                _logger.LogInformation("Searching tenders for keyword: {Keyword}", keyword);

                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GosPlanApiResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.Data == null)
                {
                    _logger.LogWarning("No data returned from API for keyword: {Keyword}", keyword);
                    return new List<GosPlanTender>();
                }

                _logger.LogInformation("Found {Count} tenders for keyword: {Keyword}", result.Data.Count, keyword);
                return result.Data.Select(t => MapToTender(t, keyword, queryId)).ToList();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed for keyword: {Keyword}", keyword);
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing failed for keyword: {Keyword}", keyword);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error searching tenders for keyword: {Keyword}", keyword);
                throw;
            }
        }

        private async Task ApplyRateLimit()
        {
            await _rateLimiter.WaitAsync();
            try
            {
                var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
                var delayNeeded = TimeSpan.FromMilliseconds(_options.RequestDelayMs) - timeSinceLastRequest;

                if (delayNeeded > TimeSpan.Zero)
                {
                    _logger.LogDebug("Rate limiting: waiting {DelayMs}ms", delayNeeded.TotalMilliseconds);
                    await Task.Delay(delayNeeded);
                }

                _lastRequestTime = DateTime.UtcNow;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        private static GosPlanTender MapToTender(GosPlanTenderData data, string keyword, int? queryId)
        {
            return new GosPlanTender
            {
                ExternalId = data.Id?.ToString() ?? Guid.NewGuid().ToString(),
                PurchaseNumber = data.PurchaseNumber ?? "N/A",
                Title = data.Title ?? "Без названия",
                CustomerName = data.Customer?.FullName,
                PublishDate = data.PublishDate.HasValue 
                    ? DateTimeOffset.FromUnixTimeMilliseconds(data.PublishDate.Value).UtcDateTime 
                    : null,
                DirectLinkToSource = data.Links?.FirstOrDefault()?.Href,
                SearchKeyword = keyword,
                FoundByQueryId = queryId
            };
        }
    }

    // Модели для десериализации JSON ответа от ГосПлан API
    public class GosPlanApiResponse
    {
        public List<GosPlanTenderData> Data { get; set; } = new List<GosPlanTenderData>();
        public int Total { get; set; }
    }

    public class GosPlanTenderData
    {
        public long? Id { get; set; }
        public string? PurchaseNumber { get; set; }
        public string? Title { get; set; }
        public long? PublishDate { get; set; }
        public GosPlanCustomer? Customer { get; set; }
        public List<GosPlanLink>? Links { get; set; }
    }

    public class GosPlanCustomer
    {
        public string? FullName { get; set; }
    }

    public class GosPlanLink
    {
        public string? Href { get; set; }
        public string? Rel { get; set; }
    }

    public class GosPlanTender
    {
        public string ExternalId { get; set; } = string.Empty;
        public string PurchaseNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public DateTime? PublishDate { get; set; }
        public string? DirectLinkToSource { get; set; }
        public string SearchKeyword { get; set; } = string.Empty;
        public int? FoundByQueryId { get; set; }
    }
}
