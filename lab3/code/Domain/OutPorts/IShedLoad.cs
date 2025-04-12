using Domain.Models;
namespace Domain.OutPorts;

public interface IShedLoad
{
    //TODO
    public List<Event> LoadShedule(string user_name, string path);
}