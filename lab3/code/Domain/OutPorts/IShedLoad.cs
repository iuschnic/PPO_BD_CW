namespace Domain.OutPorts;

public interface IShedLoad
{
    public List<Event> LoadShedule(Guid user_id);
}