using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Storage.Models;

namespace Storage.PostgresStorageAdapters;

public class PostgresDBContext : DbContext
{
    public DbSet<DBUser> Users { get; set; }
    public DbSet<DBEvent> Events { get; set; }
    public DbSet<DBHabit> Habits { get; set; }
    public DbSet<DBActualTime> ActualTimes { get; set; }
    public DbSet<DBPrefFixedTime> PrefFixedTimes { get; set; }
    public DbSet<DBSTime> SettingsTimes { get; set; }
    public DbSet<DBUserSettings> Settings { get; set; }
    public PostgresDBContext()
    {
        Database.EnsureCreated();
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=usersdb;Username=postgres;Password=postgres");
    }
}
