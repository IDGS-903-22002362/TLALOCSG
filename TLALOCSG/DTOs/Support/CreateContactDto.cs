using System.ComponentModel.DataAnnotations;

public class CreateContactDto
{
    [Required, StringLength(150)] public string FullName { get; set; } = null!;
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = null!;
    [StringLength(30)] public string? Phone { get; set; }
    [StringLength(60)] public string Topic { get; set; } = "General";
    [Required, StringLength(3000)] public string Message { get; set; } = null!;
    public bool AsTicket { get; set; } = false;  // si viene logueado y marca “Abrir ticket”
}