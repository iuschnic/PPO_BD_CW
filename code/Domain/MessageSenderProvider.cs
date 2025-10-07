using Domain.InPorts;
using Domain.OutPorts;
using Domain.Models;

namespace Domain;

public class MessageSenderProvider(IHabitRepo habitRepo,
    IUserRepo userRepo) : IMessageSenderProvider
{
    private readonly IHabitRepo _habitRepo = habitRepo;
    private readonly IUserRepo _userRepo = userRepo;

    public async Task<List<UserHabitInfo>> GetUsersToNotifyAsync()
    {
        var result = await _habitRepo.GetUsersToNotifyAsync();
        return result ?? throw new Exception("Ошибка получения списка привычек, которые в скором времени нужно выполнить");
    }
    public async Task<bool> TryLogInAsync(string login, string password)
    {
        return await _userRepo.TryLogInAsync(login, password);
    }
}
