using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Types;

namespace Domain.InPorts;

public interface IInternalTaskTracker
{
    Task<User> CreateUserAsync(string username, PhoneNumber phone_number, string password);
}
