using Domain.Exceptions;
using Domain.InPorts;
using Microsoft.AspNetCore.Mvc;
using TaskTrackerDtoModels;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/v1/users/{username}")]
public class UsersController : ControllerBase
{
    private readonly ITaskTracker _taskTracker;
    private readonly ILogger<UsersController> _logger;

    public UsersController(ITaskTracker taskTracker, ILogger<UsersController> logger)
    {
        _taskTracker = taskTracker;
        _logger = logger;
    }

    [HttpDelete]
    public async Task<ActionResult> DeleteUser(string username)
    {
        try
        {
            await _taskTracker.DeleteUserAsync(username);

            _logger.LogInformation("User {UserName} deleted successfully", username);
            return NoContent();
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning("User {UserName} not found for deletion", username);
            return NotFound(new ErrorResponseDto
            {
                Error = "USER_NOT_FOUND",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (RepositoryOperationException ex)
        {
            _logger.LogError(ex, "Repository error during user deletion for {UserName}", username);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "REPOSITORY_ERROR",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during user deletion for {UserName}", username);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "INTERNAL_ERROR",
                Message = "Внутренняя ошибка сервера",
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
