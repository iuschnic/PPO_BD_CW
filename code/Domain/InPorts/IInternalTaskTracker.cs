using Domain.Models;
using Types;

namespace Domain.InPorts;

public interface IInternalTaskTracker
{
    Task<User> CreateUserAsync(string username, PhoneNumber phone_number, string password);
}
