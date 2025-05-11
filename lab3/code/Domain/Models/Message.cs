namespace Domain.Models;

public class Message
{
    public Guid Id { get; }
    public string Text { get; }
    public DateOnly DateSent { get; }
    public Message(Guid id, string text, DateOnly dateSent)
    {
        Id = id;
        Text = text;
        DateSent = dateSent;
    }
}
