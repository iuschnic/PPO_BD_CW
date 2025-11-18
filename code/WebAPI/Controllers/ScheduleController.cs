using Domain.Exceptions;
using Domain.InPorts;
using Microsoft.AspNetCore.Mvc;
using TaskTrackerDtoModels;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/v1/users/{username}/schedule")]
public class ScheduleController : ControllerBase
{
    private readonly ITaskTracker _taskTracker;
    private readonly ILogger<ScheduleController> _logger;

    public ScheduleController(ITaskTracker taskTracker, ILogger<ScheduleController> logger)
    {
        _taskTracker = taskTracker;
        _logger = logger;
    }

    [HttpPost("import")]
    public async Task<ActionResult<DistributionResultDto>> ImportSchedule(string username, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ErrorResponseDto
                {
                    Error = "VALIDATION_ERROR",
                    Message = "Файл не был предоставлен",
                    Timestamp = DateTime.UtcNow
                });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".csv" && extension != ".ics")
            {
                return BadRequest(new ErrorResponseDto
                {
                    Error = "VALIDATION_ERROR",
                    Message = "Поддерживаются только файлы CSV и ICS формата",
                    Timestamp = DateTime.UtcNow
                });
            }

            using var stream = file.OpenReadStream();
            var (user, nonDistributedHabits) = await _taskTracker.ImportNewSheduleAsync(username, stream, extension);
            var resultDto = DtoMapper.MapToDto((user, nonDistributedHabits));

            _logger.LogInformation("Schedule imported for user {UserName} from file {FileName}", username, file.FileName);
            return Ok(resultDto);
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning("User {UserName} not found when importing schedule", username);
            return NotFound(new ErrorResponseDto
            {
                Error = "USER_NOT_FOUND",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (ScheduleLoadException ex)
        {
            _logger.LogWarning("Schedule load error for user {UserName}: {Message}", username, ex.Message);
            return BadRequest(new ErrorResponseDto
            {
                Error = "SCHEDULE_LOAD_ERROR",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (HabitsNotFoundException ex)
        {
            _logger.LogError(ex, "Habits not found for user {UserName} when importing schedule", username);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "HABITS_NOT_FOUND",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (RepositoryOperationException ex)
        {
            _logger.LogError(ex, "Repository error when importing schedule for user {UserName}", username);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "REPOSITORY_ERROR",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when importing schedule for user {UserName}", username);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "INTERNAL_ERROR",
                Message = "Внутренняя ошибка сервера",
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
