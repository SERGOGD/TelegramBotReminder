using System.ComponentModel.DataAnnotations;

public class Reminder
{
    [Key]
    public int Id { get; set; }
    public long tgId { get; set; }
    public string? ReminderText { get; set; }
    public DateTime ReminderTime { get; set; }
}
