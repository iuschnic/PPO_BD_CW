using Domain.Exceptions;
using Domain.InPorts;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using WebCLI.Models;

namespace WebCLI.Controllers;

[ApiController]
[Route("api/v1/users/{username}/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly ITaskTracker _taskTracker;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(ITaskTracker taskTracker, ILogger<NotificationsController> logger)
    {
        _taskTracker = taskTracker;
        _logger = logger;
    }

    /*[HttpPatch("on")]
    public async Task<ActionResult<UserDto>> TurnOnNotifications(string username)
    {
        try
        {
            var user = await _taskTracker.NotificationsOnAsync(username);
            var userDto = DtoMapper.MapToDto(user);

            _logger.LogInformation("Notifications turned on for user {UserName}", username);
            return Ok(userDto);
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning("User {UserName} not found when turning on notifications", username);
            return NotFound(new ErrorResponseDto
            {
                Error = "USER_NOT_FOUND",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (RepositoryOperationException ex)
        {
            _logger.LogError(ex, "Repository error when turning on notifications for user {UserName}", username);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "REPOSITORY_ERROR",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (EventsNotFoundException ex)
        {
            _logger.LogError(ex, "Events not found for user {UserName} when turning on notifications", username);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "EVENTS_NOT_FOUND",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when turning on notifications for user {UserName}", username);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "INTERNAL_ERROR",
                Message = "Внутренняя ошибка сервера",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpPatch("off")]
    public async Task<ActionResult<UserDto>> TurnOffNotifications(string username)
    {
        try
        {
            var user = await _taskTracker.NotificationsOffAsync(username);
            var userDto = DtoMapper.MapToDto(user);

            _logger.LogInformation("Notifications turned off for user {UserName}", username);
            return Ok(userDto);
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning("User {UserName} not found when turning off notifications", username);
            return NotFound(new ErrorResponseDto
            {
                Error = "USER_NOT_FOUND",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (RepositoryOperationException ex)
        {
            _logger.LogError(ex, "Repository error when turning off notifications for user {UserName}", username);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "REPOSITORY_ERROR",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (EventsNotFoundException ex)
        {
            _logger.LogError(ex, "Events not found for user {UserName} when turning off notifications", username);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "EVENTS_NOT_FOUND",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when turning off notifications for user {UserName}", username);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "INTERNAL_ERROR",
                Message = "Внутренняя ошибка сервера",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpPut("timings")]
    public async Task<ActionResult<UserDto>> UpdateNotificationTimings(string username, [FromBody] List<NotificationTimingDto> timings)
    {
        try
        {
            var timeTuples = DtoMapper.MapToTimeTuples(timings);
            var user = await _taskTracker.UpdateNotificationTimingsAsync(timeTuples, username);
            var userDto = DtoMapper.MapToDto(user);

            _logger.LogInformation("Notification timings updated for user {UserName}", username);
            return Ok(userDto);
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning("User {UserName} not found when updating notification timings", username);
            return NotFound(new ErrorResponseDto
            {
                Error = "USER_NOT_FOUND",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (RepositoryOperationException ex)
        {
            _logger.LogError(ex, "Repository error when updating notification timings for user {UserName}", username);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "REPOSITORY_ERROR",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (EventsNotFoundException ex)
        {
            _logger.LogError(ex, "Events not found for user {UserName} when updating notification timings", username);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "EVENTS_NOT_FOUND",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when updating notification timings for user {UserName}", username);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "INTERNAL_ERROR",
                Message = "Внутренняя ошибка сервера",
                Timestamp = DateTime.UtcNow
            });
        }
    }*/

    [HttpPatch("notifications")]
    public async Task<ActionResult<UserDto>> UpdateNotificationSettings(string username,
        [FromBody] NotificationSettingsDto newSettings)
    {
        try
        {
            if (newSettings.NotifyOn == null && newSettings.NewTimings == null)
            {
                _logger.LogWarning("User {UserName} requested invalid settings patch (two null fields)", username);
                return BadRequest(new ErrorResponseDto
                {
                    Error = "VALIDATION_ERROR",
                    Message = "Хотя бы одно из полей должно быть не null для обновления",
                    Timestamp = DateTime.UtcNow
                });
            }
            var timeTuples = newSettings.NewTimings != null ? DtoMapper.MapToTimeTuples(newSettings.NewTimings) : null;
            var user = await _taskTracker.ChangeSettingsAsync(timeTuples, newSettings.NotifyOn, username);
            var userDto = DtoMapper.MapToDto(user);

            _logger.LogInformation("Settings updated for user {UserName}", username);
            return Ok(userDto);
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning("User {UserName} not found when updating settings", username);
            return NotFound(new ErrorResponseDto
            {
                Error = "USER_NOT_FOUND",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (RepositoryOperationException ex)
        {
            _logger.LogError(ex, "Repository error when updating settings for user {UserName}", username);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "REPOSITORY_ERROR",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when updating settings for user {UserName}", username);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "INTERNAL_ERROR",
                Message = "Внутренняя ошибка сервера",
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
