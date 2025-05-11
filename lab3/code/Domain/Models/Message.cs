namespace Domain.Models;

public class Message
{
    public Guid Id { get; }
    public string Text { get; }
    public DateTime TimeSent { get; }
    public Message(Guid id, string text, DateTime timeSent)
    {
        Id = id;
        Text = text;
        TimeSent = timeSent;
    }
}
