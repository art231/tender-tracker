using System.Net;
using System.Net.Mail;
using System.Text;
using TenderTracker.API.DTOs;
using TenderTracker.API.Models;

namespace TenderTracker.API.Services
{
    public class NotificationSenderService : INotificationSenderService
    {
        private readonly ILogger<NotificationSenderService> _logger;
        private readonly IConfiguration _configuration;

        public NotificationSenderService(
            ILogger<NotificationSenderService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<bool> SendNewTenderNotificationAsync(NotificationSettingsDto settings, FoundTender tender)
        {
            var subject = $"Новый тендер: {tender.Title}";
            var body = GenerateNewTenderNotificationBody(tender);
            
            return await SendNotificationAsync(settings, subject, body);
        }

        public async Task<bool> SendDeadlineApproachingNotificationAsync(NotificationSettingsDto settings, FoundTender tender)
        {
            var subject = $"Срок подачи заявок истекает: {tender.Title}";
            var body = GenerateDeadlineNotificationBody(tender);
            
            return await SendNotificationAsync(settings, subject, body);
        }

        public async Task<bool> SendTechnologyMatchNotificationAsync(NotificationSettingsDto settings, FoundTender tender, TechnologyAnalysis analysis)
        {
            var subject = $"Совпадение технологий в тендере: {tender.Title}";
            var body = GenerateTechnologyMatchNotificationBody(tender, analysis);
            
            return await SendNotificationAsync(settings, subject, body);
        }

        public async Task<bool> SendTestNotificationAsync(NotificationSettingsDto settings, string message)
        {
            var subject = "Тестовое уведомление от TenderTracker";
            var body = $"Это тестовое уведомление: {message}";
            
            return await SendNotificationAsync(settings, subject, body);
        }

        private async Task<bool> SendNotificationAsync(NotificationSettingsDto settings, string subject, string body)
        {
            try
            {
                switch (settings.NotificationType.ToLower())
                {
                    case "email":
                        return await SendEmailNotificationAsync(settings, subject, body);
                    
                    case "telegram":
                        return await SendTelegramNotificationAsync(settings, subject, body);
                    
                    case "webhook":
                        return await SendWebhookNotificationAsync(settings, subject, body);
                    
                    default:
                        _logger.LogWarning("Unknown notification type: {NotificationType}", settings.NotificationType);
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user {UserId} via {NotificationType}", 
                    settings.UserId, settings.NotificationType);
                return false;
            }
        }

        private async Task<bool> SendEmailNotificationAsync(NotificationSettingsDto settings, string subject, string body)
        {
            if (string.IsNullOrEmpty(settings.EmailAddress))
            {
                _logger.LogWarning("Email address not configured for user {UserId}", settings.UserId);
                return false;
            }

            try
            {
                var smtpConfig = _configuration.GetSection("Smtp");
                var smtpHost = smtpConfig["Host"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(smtpConfig["Port"] ?? "587");
                var smtpUsername = smtpConfig["Username"];
                var smtpPassword = smtpConfig["Password"];
                var enableSsl = bool.Parse(smtpConfig["EnableSsl"] ?? "true");

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                    EnableSsl = enableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpUsername ?? "noreply@tendertracker.com", "TenderTracker"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(settings.EmailAddress);

                await client.SendMailAsync(mailMessage);
                
                _logger.LogInformation("Email notification sent to {EmailAddress} for user {UserId}", 
                    settings.EmailAddress, settings.UserId);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {EmailAddress}", settings.EmailAddress);
                return false;
            }
        }

        private async Task<bool> SendTelegramNotificationAsync(NotificationSettingsDto settings, string subject, string body)
        {
            if (string.IsNullOrEmpty(settings.TelegramChatId))
            {
                _logger.LogWarning("Telegram chat ID not configured for user {UserId}", settings.UserId);
                return false;
            }

            try
            {
                var telegramConfig = _configuration.GetSection("Telegram");
                var botToken = telegramConfig["BotToken"];
                
                if (string.IsNullOrEmpty(botToken))
                {
                    _logger.LogWarning("Telegram bot token not configured");
                    return false;
                }

                var message = $"*{subject}*\n\n{body}";
                var apiUrl = $"https://api.telegram.org/bot{botToken}/sendMessage";
                
                using var httpClient = new HttpClient();
                var content = new
                {
                    chat_id = settings.TelegramChatId,
                    text = message,
                    parse_mode = "Markdown"
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(content);
                var response = await httpClient.PostAsync(apiUrl, 
                    new StringContent(jsonContent, Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Telegram notification sent to chat {ChatId} for user {UserId}", 
                        settings.TelegramChatId, settings.UserId);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Telegram API error: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending Telegram notification to chat {ChatId}", 
                    settings.TelegramChatId);
                return false;
            }
        }

        private async Task<bool> SendWebhookNotificationAsync(NotificationSettingsDto settings, string subject, string body)
        {
            if (string.IsNullOrEmpty(settings.WebhookUrl))
            {
                _logger.LogWarning("Webhook URL not configured for user {UserId}", settings.UserId);
                return false;
            }

            try
            {
                using var httpClient = new HttpClient();
                var content = new
                {
                    userId = settings.UserId,
                    notificationType = settings.NotificationType,
                    subject,
                    body,
                    timestamp = DateTime.UtcNow
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(content);
                var response = await httpClient.PostAsync(settings.WebhookUrl, 
                    new StringContent(jsonContent, Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Webhook notification sent to {WebhookUrl} for user {UserId}", 
                        settings.WebhookUrl, settings.UserId);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Webhook error: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending webhook notification to {WebhookUrl}", 
                    settings.WebhookUrl);
                return false;
            }
        }

        private string GenerateNewTenderNotificationBody(FoundTender tender)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<h2>Новый тендер</h2>");
            sb.AppendLine($"<p><strong>Номер закупки:</strong> {tender.PurchaseNumber}</p>");
            sb.AppendLine($"<p><strong>Название:</strong> {tender.Title}</p>");
            sb.AppendLine($"<p><strong>Заказчик:</strong> {tender.CustomerName}</p>");
            sb.AppendLine($"<p><strong>Дата публикации:</strong> {tender.PublishDate:dd.MM.yyyy HH:mm}</p>");
            sb.AppendLine($"<p><strong>Срок подачи заявок:</strong> {tender.ApplicationDeadline:dd.MM.yyyy HH:mm}</p>");
            sb.AppendLine($"<p><strong>Максимальная цена:</strong> {tender.MaxPrice:N2} руб.</p>");
            sb.AppendLine($"<p><strong>Регион:</strong> {tender.Region}</p>");
            
            if (!string.IsNullOrEmpty(tender.DirectLinkToSource))
            {
                sb.AppendLine($"<p><a href=\"{tender.DirectLinkToSource}\">Открыть тендер на zakupki.gov.ru</a></p>");
            }
            
            sb.AppendLine("<hr>");
            sb.AppendLine($"<p><small>Уведомление отправлено {DateTime.Now:dd.MM.yyyy HH:mm}</small></p>");
            
            return sb.ToString();
        }

        private string GenerateDeadlineNotificationBody(FoundTender tender)
        {
            var daysLeft = tender.ApplicationDeadline.HasValue 
                ? (tender.ApplicationDeadline.Value - DateTime.Now).Days 
                : 0;
            
            var sb = new StringBuilder();
            sb.AppendLine("<h2>Срок подачи заявок истекает</h2>");
            sb.AppendLine($"<p><strong>Осталось дней:</strong> {daysLeft}</p>");
            sb.AppendLine($"<p><strong>Номер закупки:</strong> {tender.PurchaseNumber}</p>");
            sb.AppendLine($"<p><strong>Название:</strong> {tender.Title}</p>");
            sb.AppendLine($"<p><strong>Заказчик:</strong> {tender.CustomerName}</p>");
            sb.AppendLine($"<p><strong>Срок подачи заявок:</strong> {tender.ApplicationDeadline:dd.MM.yyyy HH:mm}</p>");
            
            if (!string.IsNullOrEmpty(tender.DirectLinkToSource))
            {
                sb.AppendLine($"<p><a href=\"{tender.DirectLinkToSource}\">Открыть тендер на zakupki.gov.ru</a></p>");
            }
            
            sb.AppendLine("<hr>");
            sb.AppendLine($"<p><small>Уведомление отправлено {DateTime.Now:dd.MM.yyyy HH:mm}</small></p>");
            
            return sb.ToString();
        }

        private string GenerateTechnologyMatchNotificationBody(FoundTender tender, TechnologyAnalysis analysis)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<h2>Совпадение технологий в тендере</h2>");
            sb.AppendLine($"<p><strong>Номер закупки:</strong> {tender.PurchaseNumber}</p>");
            sb.AppendLine($"<p><strong>Название:</strong> {tender.Title}</p>");
            sb.AppendLine($"<p><strong>Заказчик:</strong> {tender.CustomerName}</p>");
            sb.AppendLine($"<p><strong>Совместимость:</strong> {(analysis.IsCompatible ? "Совместим" : "Не совместим")}</p>");
            sb.AppendLine($"<p><strong>Процент совпадения:</strong> {analysis.MatchScore}%</p>");
            
            if (!string.IsNullOrEmpty(analysis.MatchedTechnologiesJson))
            {
                sb.AppendLine("<p><strong>Найденные технологии:</strong></p>");
                sb.AppendLine($"<p>{analysis.MatchedTechnologiesJson}</p>");
            }
            
            if (!string.IsNullOrEmpty(tender.DirectLinkToSource))
            {
                sb.AppendLine($"<p><a href=\"{tender.DirectLinkToSource}\">Открыть тендер на zakupki.gov.ru</a></p>");
            }
            
            sb.AppendLine("<hr>");
            sb.AppendLine($"<p><small>Уведомление отправлено {DateTime.Now:dd.MM.yyyy HH:mm}</small></p>");
            
            return sb.ToString();
        }
    }
}
