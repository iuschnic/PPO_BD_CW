using MessageSenderStorage.Models;
using Microsoft.EntityFrameworkCore;

namespace MessageSenderStorage.EfAdapters;
public class MessageSenderDBContext : DbContext
{
    public DbSet<DBSubscriber> Subscribers { get; set; }
    public DbSet<DBMessage> Messages { get; set; }
    public DbSet<DBSubscriberMessage> SubscriberMessage { get; set; }

    public MessageSenderDBContext(DbContextOptions<MessageSenderDBContext> options) : base(options)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
        Database.EnsureCreated();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DBSubscriber>().HasKey(s => s.Id);
        modelBuilder.Entity<DBMessage>().HasKey(m => m.Id);
        modelBuilder.Entity<DBSubscriberMessage>().HasKey(sm => new { sm.MessageID, sm.SubscriberID });
    }
}
