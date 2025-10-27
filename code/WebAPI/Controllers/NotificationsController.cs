using Domain.Exceptions;
using Domain.InPorts;
using Microsoft.AspNetCore.Mvc;
using TaskTrackerDtoModels;

namespace WebAPI.Controllers;

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

    [HttpPatch("change-settings")]
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
