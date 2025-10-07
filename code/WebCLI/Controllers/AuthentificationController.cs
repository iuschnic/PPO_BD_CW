using Domain.Exceptions;
using Domain.InPorts;
using Microsoft.AspNetCore.Mvc;
using WebCLI.Models;

namespace WebCLI.Controllers;

[ApiController]
[Route("api/v1")]
public class AuthenticationController : ControllerBase
{
    private readonly ITaskTracker _taskTracker;
    private readonly ILogger<AuthenticationController> _logger;

    public AuthenticationController(ITaskTracker taskTracker, ILogger<AuthenticationController> logger)
    {
        _taskTracker = taskTracker;
        _logger = logger;
    }

    [HttpPost("auth/register")]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequestDto request)
    {
        try
        {
            var phoneNumber = DtoMapper.MapToDomain(request.PhoneNumber);
            var user = await _taskTracker.CreateUserAsync(request.UserName, phoneNumber, request.Password);
            var userDto = DtoMapper.MapToDto(user);

            _logger.LogInformation("User {UserName} registered successfully", request.UserName);
            return Ok(userDto);
        }
        catch (UserAlreadyExistsException ex)
        {
            _logger.LogWarning("User {UserName} already exists", request.UserName);
            return Conflict(new ErrorResponseDto
            {
                Error = "USER_ALREADY_EXISTS",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (RepositoryOperationException ex)
        {
            _logger.LogError(ex, "Repository error during registration for user {UserName}", request.UserName);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "REPOSITORY_ERROR",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (EventsNotFoundException ex)
        {
            _logger.LogError(ex, "Events not found for user {UserName}", request.UserName);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "EVENTS_NOT_FOUND",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration for user {UserName}", request.UserName);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "INTERNAL_ERROR",
                Message = "Внутренняя ошибка сервера",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpPost("auth/login")]
    public async Task<ActionResult<UserDto>> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var user = await _taskTracker.LogInAsync(request.UserName, request.Password);
            var userDto = DtoMapper.MapToDto(user);

            _logger.LogInformation("User {UserName} logged in successfully", request.UserName);
            return Ok(userDto);
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning("User {UserName} not found", request.UserName);
            return NotFound(new ErrorResponseDto
            {
                Error = "USER_NOT_FOUND",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (InvalidCredentialsException ex)
        {
            _logger.LogWarning("Invalid credentials for user {UserName}", request.UserName);
            return Unauthorized(new ErrorResponseDto
            {
                Error = "INVALID_CREDENTIALS",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (RepositoryOperationException ex)
        {
            _logger.LogError(ex, "Repository error during login for user {UserName}", request.UserName);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "REPOSITORY_ERROR",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (EventsNotFoundException ex)
        {
            _logger.LogError(ex, "Events not found for user {UserName}", request.UserName);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "EVENTS_NOT_FOUND",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for user {UserName}", request.UserName);
            return StatusCode(500, new ErrorResponseDto
            {
                Error = "INTERNAL_ERROR",
                Message = "Внутренняя ошибка сервера",
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
