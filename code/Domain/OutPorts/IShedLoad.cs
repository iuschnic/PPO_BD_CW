using Domain.Models;
namespace Domain.OutPorts;

public interface ISheduleLoad
{
    public List<Event> LoadDummyShedule(string user_name, string path);
    public List<Event> LoadShedule(string user_name, string path);
    public List<Event> LoadShedule(string user_name, Stream stream, string extension);
}