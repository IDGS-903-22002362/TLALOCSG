namespace TLALOCSG.DTOs.Users;
public class UserQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public bool? Active { get; set; }
    public string? Role { get; set; }
    public string? Search { get; set; }
}
