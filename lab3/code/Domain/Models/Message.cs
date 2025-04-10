namespace Domain.Models;

public class Message(Guid id, string text, DateOnly dateSent)
{
    public Guid Id { get; } = id;
    public string Text { get; } = text;
    public DateOnly DateSent { get; } = dateSent;
}
