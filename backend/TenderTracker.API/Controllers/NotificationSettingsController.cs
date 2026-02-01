using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using TenderTracker.API.DTOs;
using TenderTracker.API.Services;

namespace TenderTracker.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowAngularApp")]
    public class NotificationSettingsController : ControllerBase
    {
        private readonly INotificationSettingsService _notificationSettingsService;
        private readonly ILogger<NotificationSettingsController> _logger;

        public NotificationSettingsController(
            INotificationSettingsService notificationSettingsService,
            ILogger<NotificationSettingsController> logger)
        {
            _notificationSettingsService = notificationSettingsService;
            _logger = logger;
        }

        // GET: api/notificationsettings/{userId}
        [HttpGet("{userId}")]
        public async Task<ActionResult<NotificationSettingsDto>> GetNotificationSettings(int userId)
        {
            try
            {
                var settings = await _notificationSettingsService.GetByUserIdAsync(userId);
                
                if (settings == null)
                {
                    return NotFound($"Notification settings not found for user {userId}");
                }

                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification settings for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/notificationsettings
        [HttpPost]
        public async Task<ActionResult<NotificationSettingsDto>> CreateNotificationSettings([FromBody] CreateNotificationSettingsDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var settings = await _notificationSettingsService.CreateAsync(createDto);
                return CreatedAtAction(nameof(GetNotificationSettings), new { userId = settings.UserId }, settings);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification settings");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/notificationsettings/{userId}
        [HttpPut("{userId}")]
        public async Task<ActionResult<NotificationSettingsDto>> UpdateNotificationSettings(int userId, [FromBody] UpdateNotificationSettingsDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var settings = await _notificationSettingsService.UpdateAsync(userId, updateDto);
                return Ok(settings);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification settings for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/notificationsettings/{userId}
        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteNotificationSettings(int userId)
        {
            try
            {
                var deleted = await _notificationSettingsService.DeleteAsync(userId);
                
                if (!deleted)
                {
                    return NotFound($"Notification settings not found for user {userId}");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification settings for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/notificationsettings/active
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<NotificationSettingsDto>>> GetActiveNotificationSettings()
        {
            try
            {
                var settings = await _notificationSettingsService.GetAllActiveAsync();
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active notification settings");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/notificationsettings/{userId}/check-tender/{tenderId}
        [HttpPost("{userId}/check-tender/{tenderId}")]
        public async Task<ActionResult<bool>> ShouldNotifyForTender(int userId, int tenderId)
        {
            try
            {
                // This endpoint would need access to tender data
                // For now, we'll return a placeholder response
                // In a real implementation, we would fetch the tender and check criteria
                return Ok(new { shouldNotify = false, message = "Endpoint not fully implemented" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking notification for user {UserId} and tender {TenderId}", userId, tenderId);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
