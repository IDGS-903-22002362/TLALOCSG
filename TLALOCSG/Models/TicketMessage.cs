using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TLALOCSG.Models;

[Table("TicketMessages")]
[Index(nameof(TicketId), Name = "IX_TicketMessages_TicketId")]
public partial class TicketMessage
{
    [Key]
    public int MessageId { get; set; }

    public int TicketId { get; set; }

    [Required, StringLength(450)]
    public string SenderId { get; set; } = null!; // AspNetUsers.Id

    [Required]
    public string Body { get; set; } = null!;     // nvarchar(max)

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(TicketId))]
    public virtual Ticket Ticket { get; set; } = null!;
}
