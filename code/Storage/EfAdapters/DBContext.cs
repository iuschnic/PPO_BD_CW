using Microsoft.EntityFrameworkCore;
using Storage.Models;

namespace Storage.EfAdapters;

public interface ITaskTrackerContext
{
    public DbSet<DBUser> Users { get; }
    public DbSet<DBEvent> Events { get; }
    public DbSet<DBHabit> Habits { get; }
    public DbSet<DBActualTime> ActualTimes { get; }
    public DbSet<DBPrefFixedTime> PrefFixedTimes { get; }
    public DbSet<DBSTime> SettingsTimes { get; }
    public DbSet<DBUserSettings> USettings { get; }
    int SaveChanges();
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class EfDbContext : DbContext, ITaskTrackerContext
{
    public DbSet<DBUser> Users { get; set; }
    public DbSet<DBEvent> Events { get; set; }
    public DbSet<DBHabit> Habits { get; set; }
    public DbSet<DBActualTime> ActualTimes { get; set; }
    public DbSet<DBPrefFixedTime> PrefFixedTimes { get; set; }
    public DbSet<DBSTime> SettingsTimes { get; set; }
    public DbSet<DBUserSettings> USettings { get; set; }
    public EfDbContext(DbContextOptions<EfDbContext> options) : base(options)
    {
        Database.EnsureDeleted();
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
        Database.EnsureCreated();
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DBUser>().HasKey(u => u.NameID);
        modelBuilder.Entity<DBEvent>().HasKey(e => e.Id);
        modelBuilder.Entity<DBHabit>().HasKey(h => h.Id);
        modelBuilder.Entity<DBActualTime>().HasKey(t => t.Id);
        modelBuilder.Entity<DBPrefFixedTime>().HasKey(t => t.Id);
        modelBuilder.Entity<DBSTime>().HasKey(t => t.Id);
        modelBuilder.Entity<DBUserSettings>().HasKey(s => s.Id);
    }
}
