using Microsoft.EntityFrameworkCore;
public class ReminderContext : DbContext
{
    public DbSet<Reminder> Reminders { get; set; }
    public ReminderContext()
    {
        Database.EnsureCreated();   // гарантируем, что БД создана
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=reminders.db");
    }
}