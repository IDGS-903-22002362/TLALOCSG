namespace TLALOCSG.DTOs.Users
{
    public class UserCreateDto
    {
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Role { get; set; } = "Client";
        public string? PhoneNumber { get; set; }
        public string? Password { get; set; }
    }
}
