namespace TLALOCSG.DTOs.Users;
public record UserQueryDto(int Page = 1,
                           int PageSize = 10,
                           string? Role = null,
                           bool? Active = null);
