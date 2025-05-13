using Domain.Models;
namespace Domain.OutPorts;

public interface ISheduleLoad
{
    public List<Event> LoadShedule(string user_name, string path);
}