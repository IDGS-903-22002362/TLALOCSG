// Models/ContactMessage.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TLALOCSG.Models;

public class ContactMessage
{
    [Key] public int ContactMessageId { get; set; }

    [StringLength(150), Required] public string FullName { get; set; } = null!;
    [StringLength(150), EmailAddress, Required] public string Email { get; set; } = null!;
    [StringLength(30)] public string? Phone { get; set; }

    [StringLength(60)] public string Topic { get; set; } = "General"; // General | Ventas | Soporte
    [StringLength(3000), Required] public string Message { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(30)] public string Status { get; set; } = "New";   // New | InProgress | Closed

    public string? UserId { get; set; }
    [ForeignKey(nameof(UserId))] public ApplicationUser? User { get; set; }
}
