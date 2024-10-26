using Microsoft.EntityFrameworkCore;
public class ReminderContext : DbContext
{
    public DbSet<Reminder> Reminders { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=reminders.db");
    }
}