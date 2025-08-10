using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TLALOCSG.Models;

public class InstallTier
{
    [Key] public int Id { get; set; }

    [Range(0, int.MaxValue)]
    public int MinQty { get; set; }

    public int? MaxQty { get; set; }   // null = “en adelante”

    [Column(TypeName = "decimal(18,2)")]
    public decimal BaseCost { get; set; }
}
