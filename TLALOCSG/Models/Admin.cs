using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TLALOCSG.Models;

public partial class Admin
{
    [Key]
    public string AdminId { get; set; } = null!;

    [StringLength(150)]
    public string FullName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
}
