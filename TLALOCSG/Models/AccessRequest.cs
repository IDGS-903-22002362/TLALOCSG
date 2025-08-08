using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TLALOCSG.Models;

[Table("AccessRequests")]
[Index(nameof(Email), Name = "IX_AccessRequests_Email")]
[Index(nameof(Email), nameof(Status), Name = "IX_AccessRequests_Email_Status")] // útil para filtrar
public partial class AccessRequest
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(256)]
    public string Email { get; set; } = null!;

    [StringLength(150)]
    public string? FullName { get; set; }

    // Pending | Approved | Rejected
    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    // Guarda el Id del admin que procesó (Identity usa nvarchar(450) como PK)
    [StringLength(450)]
    public string? ProcessedBy { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}
