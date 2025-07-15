using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TLALOCSG.Models;

public partial class Ticket
{
    [Key]
    public int TicketId { get; set; }

    [StringLength(450)]
    public string CustomerId { get; set; } = null!;

    [StringLength(200)]
    public string Subject { get; set; } = null!;

    public string Message { get; set; } = null!;

    [StringLength(30)]
    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    [ForeignKey("CustomerId")]
    [InverseProperty("Tickets")]
    public virtual Customer Customer { get; set; } = null!;
}
