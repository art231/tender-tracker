using Microsoft.AspNetCore.Mvc;
using TenderTracker.API.DTOs;
using TenderTracker.API.Services;

namespace TenderTracker.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentDownloadTasksController : ControllerBase
    {
        private readonly IDocumentDownloadTaskService _taskService;
        private readonly ILogger<DocumentDownloadTasksController> _logger;

        public DocumentDownloadTasksController(
            IDocumentDownloadTaskService taskService,
            ILogger<DocumentDownloadTasksController> logger)
        {
            _taskService = taskService;
            _logger = logger;
        }

        /// <summary>
        /// Получить все задачи загрузки документов
        /// </summary>
        /// <param name="tenderId">ID тендера (опционально)</param>
        /// <param name="status">Статус задачи (опционально)</param>
        /// <param name="docType">Тип документа (опционально)</param>
        [HttpGet]
        public async Task<ActionResult<List<DocumentDownloadTaskDto>>> GetTasks(
            [FromQuery] int? tenderId = null,
            [FromQuery] string? status = null,
            [FromQuery] string? docType = null)
        {
            try
            {
                var tasks = await _taskService.GetTasksAsync(tenderId, status, docType);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document download tasks");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Получить задачу по ID
        /// </summary>
        /// <param name="id">ID задачи</param>
        [HttpGet("{id}")]
        public async Task<ActionResult<DocumentDownloadTaskDto>> GetTask(int id)
        {
            try
            {
                var task = await _taskService.GetTaskByIdAsync(id);
                if (task == null)
                    return NotFound($"Task with ID {id} not found");

                return Ok(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task by ID: {TaskId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Создать новую задачу загрузки документов
        /// </summary>
        /// <param name="createDto">Данные для создания задачи</param>
        [HttpPost]
        public async Task<ActionResult<DocumentDownloadTaskDto>> CreateTask(CreateDocumentDownloadTaskDto createDto)
        {
            try
            {
                var task = await _taskService.CreateTaskAsync(createDto);
                return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document download task");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Обновить задачу
        /// </summary>
        /// <param name="id">ID задачи</param>
        /// <param name="updateDto">Данные для обновления</param>
        [HttpPut("{id}")]
        public async Task<ActionResult<DocumentDownloadTaskDto>> UpdateTask(int id, UpdateDocumentDownloadTaskDto updateDto)
        {
            try
            {
                var task = await _taskService.UpdateTaskAsync(id, updateDto);
                if (task == null)
                    return NotFound($"Task with ID {id} not found");

                return Ok(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task: {TaskId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Удалить задачу
        /// </summary>
        /// <param name="id">ID задачи</param>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            try
            {
                var deleted = await _taskService.DeleteTaskAsync(id);
                if (!deleted)
                    return NotFound($"Task with ID {id} not found");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task: {TaskId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Повторить выполнение задачи (для задач со статусом Failed)
        /// </summary>
        /// <param name="id">ID задачи</param>
        [HttpPost("{id}/retry")]
        public async Task<IActionResult> RetryTask(int id)
        {
            try
            {
                var retried = await _taskService.RetryTaskAsync(id);
                if (!retried)
                    return NotFound($"Task with ID {id} not found or not in Failed status");

                return Ok(new { message = "Task retry scheduled successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying task: {TaskId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Отменить задачу
        /// </summary>
        /// <param name="id">ID задачи</param>
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelTask(int id)
        {
            try
            {
                var cancelled = await _taskService.CancelTaskAsync(id);
                if (!cancelled)
                    return NotFound($"Task with ID {id} not found or already completed/cancelled");

                return Ok(new { message = "Task cancelled successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling task: {TaskId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Получить статистику по задачам
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<DocumentDownloadTaskStatsDto>> GetStats()
        {
            try
            {
                var stats = await _taskService.GetStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task statistics");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Получить ожидающие задачи (для обработки фоновыми сервисами)
        /// </summary>
        /// <param name="limit">Максимальное количество задач (по умолчанию 100)</param>
        [HttpGet("pending")]
        public async Task<ActionResult<List<DocumentDownloadTaskDto>>> GetPendingTasks([FromQuery] int limit = 100)
        {
            try
            {
                var tasks = await _taskService.GetPendingTasksAsync(limit);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending tasks");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Обработать задачу (для фоновых сервисов)
        /// </summary>
        /// <param name="id">ID задачи</param>
        [HttpPost("{id}/process")]
        public async Task<IActionResult> ProcessTask(int id)
        {
            try
            {
                var processed = await _taskService.ProcessTaskAsync(id);
                if (!processed)
                    return NotFound($"Task with ID {id} not found or not in Pending status");

                return Ok(new { message = "Task processed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing task: {TaskId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Создать задачи загрузки документов для тендера
        /// </summary>
        /// <param name="tenderId">ID тендера</param>
        /// <param name="docTypes">Типы документов для загрузки</param>
        [HttpPost("tender/{tenderId}")]
        public async Task<ActionResult<List<DocumentDownloadTaskDto>>> CreateTasksForTender(
            int tenderId,
            [FromBody] List<string> docTypes)
        {
            try
            {
                var createdTasks = new List<DocumentDownloadTaskDto>();

                foreach (var docType in docTypes)
                {
                    var createDto = new CreateDocumentDownloadTaskDto
                    {
                        TenderId = tenderId,
                        DocType = docType,
                        Priority = docType == "notification" ? "high" : "normal"
                    };

                    try
                    {
                        var task = await _taskService.CreateTaskAsync(createDto);
                        createdTasks.Add(task);
                    }
                    catch (InvalidOperationException)
                    {
                        // Игнорируем, если задача уже существует
                    }
                }

                return Ok(createdTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tasks for tender: {TenderId}", tenderId);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
