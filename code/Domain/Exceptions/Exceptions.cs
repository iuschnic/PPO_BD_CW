namespace Domain.Exceptions;

public abstract class TaskTrackerException : Exception
{
    public TaskTrackerException(string message) : base(message) { }
    public TaskTrackerException(string message, Exception innerException) : base(message, innerException) { }
}

public class UserNotFoundException : TaskTrackerException
{
    public string UserName { get; }

    public UserNotFoundException(string userName) 
        : base($"Пользователя с именем {userName} не существует в базе данных")
    {
        UserName = userName;
    }
}

public class UserAlreadyExistsException : TaskTrackerException
{
    public string UserName { get; }

    public UserAlreadyExistsException(string userName) 
        : base($"Аккаунт не был создан так как уже существует аккаунт с именем пользователя {userName}")
    {
        UserName = userName;
    }
}

public class InvalidCredentialsException : TaskTrackerException
{
    public string UserName { get; }

    public InvalidCredentialsException(string userName) 
        : base($"Вход в аккаунт {userName} не был выполнен так как пользователь ввел неправильный пароль")
    {
        UserName = userName;
    }
}

public class ScheduleLoadException : TaskTrackerException
{
    public string UserName { get; }
    public string FilePath { get; }

    public ScheduleLoadException(string userName, string filePath, string errorDetails) 
        : base($"Ошибка загрузки расписания для пользователя {userName} из файла {filePath}: {errorDetails}")
    {
        UserName = userName;
        FilePath = filePath;
    }

    public ScheduleLoadException(string userName, Stream stream, string errorDetails) 
        : base($"Ошибка загрузки расписания для пользователя {userName} из переданного потока: {errorDetails}")
    {
        UserName = userName;
        FilePath = "From Stream";
    }
}

public class RepositoryOperationException : TaskTrackerException
{
    public string Operation { get; }
    public string EntityType { get; }
    public string UserName { get; }

    public RepositoryOperationException(string operation, string entityType, string userName, string errorDetails = "") 
        : base($"Ошибка при выполнении операции '{operation}' для {entityType} пользователя {userName}: {errorDetails}")
    {
        Operation = operation;
        EntityType = entityType;
        UserName = userName;
    }
}

public class HabitsNotFoundException : TaskTrackerException
{
    public string UserName { get; }

    public HabitsNotFoundException(string userName) 
        : base($"Не удалось получить привычки для пользователя {userName}")
    {
        UserName = userName;
    }
}

public class EventsNotFoundException : TaskTrackerException
{
    public string UserName { get; }

    public EventsNotFoundException(string userName) 
        : base($"Не удалось получить события для пользователя {userName}")
    {
        UserName = userName;
    }
}

public class HabitValidationException : TaskTrackerException
{
    public string HabitName { get; }
    public string ValidationRule { get; }

    public HabitValidationException(string habitName, string validationRule) 
        : base($"Ошибка валидации привычки {habitName}: {validationRule}")
    {
        HabitName = habitName;
        ValidationRule = validationRule;
    }
}
