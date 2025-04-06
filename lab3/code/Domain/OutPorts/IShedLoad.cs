namespace Domain.OutPorts;

public interface IShedLoad
{
    //TODO
    public List<Event> LoadShedule(Guid user_id, string path);
}