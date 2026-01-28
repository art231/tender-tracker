using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TenderTracker.API.Models;

namespace TenderTracker.API.Clients
{
    public class GosPlanApiOptions
    {
        public string BaseUrl { get; set; } = "https://v2.gosplan.info";
        public int RequestDelayMs { get; set; } = 1000; // 1 запрос в секунду
        public int MaxRetries { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 30;
    }

    public interface IGosPlanApiClient
    {
        Task<List<GosPlanTender>> SearchTendersAsync(string keyword, int? queryId = null);
        
        Task<List<GosPlanTender>> SearchTendersAdvancedAsync(
            string keyword, 
            int? queryId = null,
            DateTime? applicationDeadlineFrom = null,
            DateTime? applicationDeadlineTo = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? region = null,
            int limit = 100);
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
            // Для обратной совместимости используем базовый поиск
            return await SearchTendersAdvancedAsync(keyword, queryId, limit: 50);
        }
        
        public async Task<List<GosPlanTender>> SearchTendersAdvancedAsync(
            string keyword, 
            int? queryId = null,
            DateTime? applicationDeadlineFrom = null,
            DateTime? applicationDeadlineTo = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? region = null,
            int limit = 100)
        {
            await ApplyRateLimit();

            try
            {
                // Поиск по 44-ФЗ с расширенными параметрами
                var endpoint44 = BuildSearchEndpoint("/fz44/purchases", keyword, applicationDeadlineFrom, 
                    applicationDeadlineTo, minPrice, maxPrice, region, limit);
                _logger.LogInformation("Searching tenders for keyword: {Keyword} (44-FZ) with advanced filters", keyword);

                var response44 = await _httpClient.GetAsync(endpoint44);
                response44.EnsureSuccessStatusCode();

                var json44 = await response44.Content.ReadAsStringAsync();
                // Логирование для отладки
                Console.WriteLine($"DEBUG 44-FZ JSON (first 500 chars): {json44.Substring(0, Math.Min(500, json44.Length))}");
                var tenders44 = JsonSerializer.Deserialize<List<PurchaseIndex>>(json44, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<PurchaseIndex>();

                // Поиск по 223-ФЗ с расширенными параметрами
                var endpoint223 = BuildSearchEndpoint("/fz223/purchases", keyword, applicationDeadlineFrom,
                    applicationDeadlineTo, minPrice, maxPrice, region, limit);
                _logger.LogInformation("Searching tenders for keyword: {Keyword} (223-FZ) with advanced filters", keyword);

                var response223 = await _httpClient.GetAsync(endpoint223);
                response223.EnsureSuccessStatusCode();

                var json223 = await response223.Content.ReadAsStringAsync();
                var tenders223 = JsonSerializer.Deserialize<List<PurchaseIndex>>(json223, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<PurchaseIndex>();

                var allTenders = tenders44.Concat(tenders223).ToList();
                
                if (!allTenders.Any())
                {
                    _logger.LogWarning("No data returned from API for keyword: {Keyword} with applied filters", keyword);
                    return new List<GosPlanTender>();
                }

                _logger.LogInformation("Found {Count} tenders for keyword: {Keyword} with filters", allTenders.Count, keyword);
                return allTenders.Select(t => MapToTender(t, keyword, queryId)).ToList();
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
        
        private string BuildSearchEndpoint(
            string baseEndpoint,
            string keyword,
            DateTime? applicationDeadlineFrom,
            DateTime? applicationDeadlineTo,
            decimal? minPrice,
            decimal? maxPrice,
            int? region,
            int limit)
        {
            var parameters = new List<string>
            {
                $"object_info_purchase={Uri.EscapeDataString(keyword)}",
                $"limit={limit}"
            };
            
            // Добавляем фильтр по сроку подачи заявок
            if (applicationDeadlineFrom.HasValue)
            {
                parameters.Add($"collecting_finished_after={applicationDeadlineFrom.Value:yyyy-MM-ddTHH:mm:ssZ}");
            }
            
            if (applicationDeadlineTo.HasValue)
            {
                parameters.Add($"collecting_finished_before={applicationDeadlineTo.Value:yyyy-MM-ddTHH:mm:ssZ}");
            }
            
            // Добавляем фильтр по цене
            if (minPrice.HasValue)
            {
                parameters.Add($"max_price_ge={minPrice.Value}");
            }
            
            if (maxPrice.HasValue)
            {
                parameters.Add($"max_price_le={maxPrice.Value}");
            }
            
            // Добавляем фильтр по региону
            if (region.HasValue)
            {
                parameters.Add($"region={region.Value}");
            }
            
            return $"{baseEndpoint}?{string.Join("&", parameters)}";
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

        private static GosPlanTender MapToTender(PurchaseIndex purchase, string keyword, int? queryId)
        {
            // Определяем имя заказчика: для 44-ФЗ - первый элемент массива Customers, для 223-ФЗ - поле Customer
            // Если оба пусты, используем Responsible (ИНН) или Placer
            var customerName = purchase.Customers?.FirstOrDefault() ?? purchase.Customer ?? purchase.Responsible ?? purchase.Placer;
            
            // Формируем ссылку на закупку (разные шаблоны для 44-ФЗ и 223-ФЗ)
            // Убедимся, что PurchaseNumber не null
            var purchaseNumber = purchase.PurchaseNumber;
            var isPurchaseNumberMissing = string.IsNullOrEmpty(purchaseNumber);
            if (isPurchaseNumberMissing)
            {
                purchaseNumber = "N/A";
            }
            
            var directLink = purchase.PurchaseType?.StartsWith("epNotification") == true 
                ? $"https://zakupki.gov.ru/epz/order/notice/ea44/view/common-info.html?regNumber={purchaseNumber}"
                : $"https://zakupki.gov.ru/epz/order/notice/ea223/view/common-info.html?regNumber={purchaseNumber}";

            // Создаем ExternalId: используем PurchaseNumber, если он есть, иначе комбинацию других полей
            var externalId = purchaseNumber;
            if (purchaseNumber == "N/A")
            {
                // Создаем уникальный ID на основе других полей
                var idParts = new List<string>();
                if (!string.IsNullOrEmpty(purchase.Customer)) idParts.Add($"customer:{purchase.Customer}");
                if (purchase.PublishedAt.HasValue) idParts.Add($"published:{purchase.PublishedAt.Value:yyyyMMddHHmmss}");
                if (!string.IsNullOrEmpty(purchase.ObjectInfo)) idParts.Add($"title:{purchase.ObjectInfo.GetHashCode():X}");
                
                if (idParts.Any())
                {
                    externalId = string.Join("_", idParts);
                }
                else
                {
                    externalId = Guid.NewGuid().ToString();
                }
            }
            
            // Определяем срок подачи заявок (CollectingFinishedAt для 44-ФЗ, SubmissionCloseAt для 223-ФЗ)
            DateTime? applicationDeadline = null;
            if (!string.IsNullOrEmpty(purchase.CollectingFinishedAt))
            {
                if (DateTime.TryParse(purchase.CollectingFinishedAt, out var deadline))
                {
                    applicationDeadline = EnsureUtc(deadline);
                }
            }
            else if (!string.IsNullOrEmpty(purchase.SubmissionCloseAt))
            {
                if (DateTime.TryParse(purchase.SubmissionCloseAt, out var deadline))
                {
                    applicationDeadline = EnsureUtc(deadline);
                }
            }
            
            // Формируем дополнительную информацию
            var additionalInfoParts = new List<string>();
            if (purchase.Region.HasValue)
            {
                additionalInfoParts.Add($"Регион: {purchase.Region.Value}");
            }
            if (!string.IsNullOrEmpty(purchase.Responsible))
            {
                additionalInfoParts.Add($"ИНН: {purchase.Responsible}");
            }
            var additionalInfo = additionalInfoParts.Any() ? string.Join(", ", additionalInfoParts) : null;
            
            // Формируем заголовок: если ObjectInfo пустое, используем информацию из других полей
            var title = purchase.ObjectInfo;
            if (string.IsNullOrEmpty(title))
            {
                var titleParts = new List<string>();
                if (!string.IsNullOrEmpty(purchase.PurchaseType)) titleParts.Add(purchase.PurchaseType);
                if (!string.IsNullOrEmpty(purchase.CurrencyCode) && purchase.MaxPrice.HasValue) 
                    titleParts.Add($"{purchase.MaxPrice.Value} {purchase.CurrencyCode}");
                
                title = titleParts.Any() ? string.Join(" - ", titleParts) : "Без названия";
            }
            
            // Добавляем префикс к CustomerName, если это похоже на ИНН (только цифры, длина 10 или 12)
            if (!string.IsNullOrEmpty(customerName) && customerName.All(char.IsDigit) && (customerName.Length == 10 || customerName.Length == 12))
            {
                customerName = $"ИНН: {customerName}";
            }
            
            // Преобразуем PublishedAt в UTC, если Kind Unspecified
            DateTime? publishDateUtc = purchase.PublishedAt.HasValue ? EnsureUtc(purchase.PublishedAt.Value) : null;
            
            // Логирование для отладки
            var tender = new GosPlanTender
            {
                ExternalId = externalId,
                PurchaseNumber = purchaseNumber,
                Title = title,
                CustomerName = customerName,
                PublishDate = publishDateUtc,
                DirectLinkToSource = directLink,
                SearchKeyword = keyword,
                FoundByQueryId = queryId,
                
                // Новые поля
                ApplicationDeadline = applicationDeadline,
                MaxPrice = purchase.MaxPrice,
                Region = purchase.Region?.ToString(),
                CustomerInn = purchase.Responsible,
                AdditionalInfo = additionalInfo
            };
            
            // Логируем проблемные случаи
            if (isPurchaseNumberMissing)
            {
                Console.WriteLine($"DEBUG: PurchaseNumber missing for tender. Customer: {customerName}, Title: {title}, PublishedAt: {purchase.PublishedAt}");
                Console.WriteLine($"DEBUG: Raw PurchaseNumber value: '{purchase.PurchaseNumber}'");
            }
            else
            {
                Console.WriteLine($"DEBUG: PurchaseNumber found: '{purchase.PurchaseNumber}'");
            }
            
            return tender;
        }

        private static DateTime EnsureUtc(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                // Предполагаем, что время указано в UTC (так как API возвращает UTC)
                return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }
            return dateTime.ToUniversalTime();
        }
    }

    // Модели для десериализации JSON ответа от ГосПлан API (44-ФЗ и 223-ФЗ)
    public class PurchaseIndex
    {
        [JsonPropertyName("collecting_finished_at")]
        public string? CollectingFinishedAt { get; set; }
        
        [JsonPropertyName("submission_close_at")]
        public string? SubmissionCloseAt { get; set; } // 223-ФЗ
        
        [JsonPropertyName("contract_guarantee_amount")]
        public decimal? ContractGuaranteeAmount { get; set; }
        
        [JsonPropertyName("contract_guarantee_part")]
        public decimal? ContractGuaranteePart { get; set; }
        
        [JsonPropertyName("currency_code")]
        public string? CurrencyCode { get; set; }
        
        [JsonPropertyName("customers")]
        public List<string>? Customers { get; set; } // 44-ФЗ
        
        [JsonPropertyName("customer")]
        public string? Customer { get; set; } // 223-ФЗ
        
        [JsonPropertyName("delivery_places")]
        public List<string>? DeliveryPlaces { get; set; }
        
        [JsonPropertyName("delivery_places_kladr")]
        public List<string>? DeliveryPlacesKladr { get; set; }
        
        [JsonPropertyName("doc_created_at")]
        public DateTime? DocCreatedAt { get; set; }
        
        [JsonPropertyName("doc_updated_at")]
        public DateTime? DocUpdatedAt { get; set; }
        
        [JsonPropertyName("ikzs")]
        public List<string>? Ikzs { get; set; }
        
        [JsonPropertyName("ktru")]
        public List<string>? Ktru { get; set; }
        
        [JsonPropertyName("max_price")]
        public decimal? MaxPrice { get; set; }
        
        [JsonPropertyName("object_info")]
        public string? ObjectInfo { get; set; }
        
        [JsonPropertyName("okpd2")]
        public List<string>? Okpd2 { get; set; }
        
        [JsonPropertyName("owners")]
        public List<string>? Owners { get; set; }
        
        [JsonPropertyName("placer")]
        public string? Placer { get; set; } // 223-ФЗ
        
        [JsonPropertyName("plan_numbers")]
        public List<string>? PlanNumbers { get; set; }
        
        [JsonPropertyName("position_numbers")]
        public List<string>? PositionNumbers { get; set; }
        
        [JsonPropertyName("published_at")]
        public DateTime? PublishedAt { get; set; }
        
        [JsonPropertyName("purchase_number")]
        public string? PurchaseNumber { get; set; }
        
        [JsonPropertyName("purchase_type")]
        public string? PurchaseType { get; set; }
        
        [JsonPropertyName("region")]
        public int? Region { get; set; }
        
        [JsonPropertyName("responsible")]
        public string? Responsible { get; set; }
        
        [JsonPropertyName("stage")]
        public int? Stage { get; set; }
        
        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
        
        [JsonPropertyName("docs")]
        public List<PurchaseDocument>? Docs { get; set; }
    }

    public class PurchaseDocument
    {
        public string? DocType { get; set; }
        public DateTime? PublishedAt { get; set; }
    }

    // Старые модели оставлены для обратной совместимости (можно удалить после рефакторинга)
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
        
        // Новые поля
        public DateTime? ApplicationDeadline { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Region { get; set; }
        public string? CustomerInn { get; set; }
        public string? AdditionalInfo { get; set; }
    }
}
