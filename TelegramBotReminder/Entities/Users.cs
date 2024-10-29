using System.ComponentModel.DataAnnotations;

public class Users
{
    [Key]
    public long tgUserId { get; set; }
    public long tgChatId { get; set; }
}