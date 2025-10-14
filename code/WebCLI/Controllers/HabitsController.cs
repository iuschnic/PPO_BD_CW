using Domain.Exceptions;
using Domain.InPorts;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using WebCLI.Models;

namespace WebCLI.Controllers;

[ApiController]
[Route("api/v1/users/{username}/habits")]
public class HabitsController : ControllerBase
{
    private readonly ITaskTracker _taskTracker;
    private readonly ILogger<HabitsController> _logger;

    public HabitsController(ITaskTracker taskTracker, ILogger<HabitsController> logger)
    {
        _taskTracker = taskTracker;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<DistributionResultDto>> AddHabit(string username, [FromBody] HabitDataDto habitData)
    {
        try
        {
            if (habitData.UserNameID != username)
            {
                return BadRequest(new ErrorResponseDto
                {
                    Error = "VALIDATION_ERROR",
                    Message = "Имя пользователя в пути и в данных привычки не совпадают",
                    Timestamp = DateTime.UtcNow
                });
            }

            var habitId = Guid.NewGuid();
            var prefFixedTimings = habitData.PrefFixedTimings
                .Select(t => DtoMapper.MapToDomain(t, Guid.NewGuid(), habitId))
                .ToList();

            var habit = DtoMapper.MapToDomain(habitData, habitId, new List<ActualTime>(), prefFixedTimings);

            var (user, nonDistributedHabits) = await _taskTracker.AddHabitAsync(habit);
            var resultDto = DtoMapper.MapToDto((user, nonDistributedHabits));

            _logger.LogInformation("Habit {HabitName} added for user {UserName}", habitData.Name, username);
            return Ok(resultDto);
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning("User {UserName} not found when adding habit", username);
            return NotFound(new ErrorResponseDto
            {
                Error = "USER_NOT_FOUND",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (EventsNotFoundException ex)
        {
            _logger.LogError(ex, "Events not found for user {UserName} when adding habit", username);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "EVENTS_NOT_FOUND",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (HabitsNotFoundException ex)
        {
            _logger.LogError(ex, "Habits not found for user {UserName} when adding habit", username);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "HABITS_NOT_FOUND",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (RepositoryOperationException ex)
        {
            _logger.LogError(ex, "Repository error when adding habit for user {UserName}", username);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "REPOSITORY_ERROR",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error when adding habit for user {UserName}", username);
            return BadRequest(new ErrorResponseDto
            {
                Error = "VALIDATION_ERROR",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when adding habit for user {UserName}", username);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "INTERNAL_ERROR",
                Message = "Внутренняя ошибка сервера",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpDelete]
    public async Task<ActionResult<DistributionResultDto>> DeleteAllHabits(string username)
    {
        try
        {
            var (user, nonDistributedHabits) = await _taskTracker.DeleteHabitsAsync(username);
            var resultDto = DtoMapper.MapToDto((user, nonDistributedHabits));

            _logger.LogInformation("All habits deleted for user {UserName}", username);
            return Ok(resultDto);
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning("User {UserName} not found when deleting habits", username);
            return NotFound(new ErrorResponseDto
            {
                Error = "USER_NOT_FOUND",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (HabitsNotFoundException ex)
        {
            _logger.LogError(ex, "Habits not found for user {UserName} when deleting habits", username);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "HABITS_NOT_FOUND",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (RepositoryOperationException ex)
        {
            _logger.LogError(ex, "Repository error when deleting habits for user {UserName}", username);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "REPOSITORY_ERROR",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when deleting habits for user {UserName}", username);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "INTERNAL_ERROR",
                Message = "Внутренняя ошибка сервера",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpDelete("{habitName}")]
    public async Task<ActionResult<DistributionResultDto>> DeleteHabit(string username, string habitName)
    {
        try
        {
            var (user, nonDistributedHabits) = await _taskTracker.DeleteHabitAsync(username, habitName);
            var resultDto = DtoMapper.MapToDto((user, nonDistributedHabits));

            _logger.LogInformation("Habit {HabitName} deleted for user {UserName}", habitName, username);
            return Ok(resultDto);
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning("User {UserName} not found when deleting habit {HabitName}", username, habitName);
            return NotFound(new ErrorResponseDto
            {
                Error = "USER_NOT_FOUND",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (EventsNotFoundException ex)
        {
            _logger.LogError(ex, "Events not found for user {UserName} when deleting habit {HabitName}", username, habitName);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "EVENTS_NOT_FOUND",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (HabitsNotFoundException ex)
        {
            _logger.LogError(ex, "Habits not found for user {UserName} when deleting habit {HabitName}", username, habitName);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "HABITS_NOT_FOUND",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (RepositoryOperationException ex)
        {
            _logger.LogError(ex, "Repository error when deleting habit {HabitName} for user {UserName}", habitName, username);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "REPOSITORY_ERROR",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when deleting habit {HabitName} for user {UserName}", habitName, username);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "INTERNAL_ERROR",
                Message = "Внутренняя ошибка сервера",
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
