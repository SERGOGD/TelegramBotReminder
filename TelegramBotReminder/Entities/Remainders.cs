public class Reminder
{
    public int Id { get; set; }
    public string? ReminderText { get; set; }
    public DateTime ReminderTime { get; set; }
    public bool IsSent { get; set; }
}
