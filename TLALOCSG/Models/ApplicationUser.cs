using Microsoft.AspNetCore.Identity;

namespace TLALOCSG.Models
{
    public class ApplicationUser : IdentityUser
    {
        // ───────── propiedades extra (opcional) ─────────
        public string? FullName { get; set; }
        public bool IsActive { get; set; } = true;

        // Auditoría mínima
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
