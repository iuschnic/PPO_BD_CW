using Domain.InPorts;
using Microsoft.AspNetCore.Mvc;
using WebCLI.Models;

namespace WebCLI.Controllers;

[ApiController]
[Route("api/v1/internal")]
public class InternalController : ControllerBase
{
    private readonly ITaskTracker _taskTracker;
    private readonly IMessageSenderProvider _messageSenderProvider;
    private readonly ILogger<InternalController> _logger;

    public InternalController(
        ITaskTracker taskTracker,
        IMessageSenderProvider messageSenderProvider,
        ILogger<InternalController> logger)
    {
        _taskTracker = taskTracker;
        _messageSenderProvider = messageSenderProvider;
        _logger = logger;
    }

    [HttpPut("check-log-in")]
    public async Task<ActionResult> CheckLogin([FromBody] LoginRequestDto request)
    {
        try
        {
            var isValid = await _messageSenderProvider.CheckLogInAsync(request.UserName, request.Password);

            if (isValid)
            {
                _logger.LogDebug("Login check successful for user {UserName}", request.UserName);
                return NoContent();
            }
            else
            {
                _logger.LogWarning("Login check failed for user {UserName}", request.UserName);
                return Unauthorized(new ErrorResponseDto
                {
                    Error = "INVALID_CREDENTIALS",
                    Message = $"Вход в аккаунт {request.UserName} не был выполнен так как пользователь ввел неправильный пароль",
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login check for user {UserName}", request.UserName);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "REPOSITORY_ERROR",
                Message = $"Ошибка при проверке учетных данных пользователя {request.UserName}",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpGet("get-users-to-notify")]
    public async Task<ActionResult<List<UserHabitInfoDto>>> GetUsersToNotify()
    {
        try
        {
            var usersToNotify = await _messageSenderProvider.GetUsersToNotifyAsync();
            var dtos = usersToNotify.Select(DtoMapper.MapToDto).ToList();

            _logger.LogInformation("Retrieved {Count} users for notification", usersToNotify.Count);
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users to notify");
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "REPOSITORY_ERROR",
                Message = "Ошибка при получении пользователей для уведомлений",
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
