using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;

namespace BEN.DTOs
{
    public abstract class BaseUserDto
    {
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public string? DisplayName { get; set; }

    }
    public class UserDto : BaseUserDto
    {
        public string? Token { get; set; }
        public UserProfileInfo ProfileInfo { get; set; }  
    }
    public class UserProfileInfo
    {
        public string? DisplayName { get; set; }
        public string? Gender { get; set; }
        public string ProfilePicturUrl { get; set; }
    }
    public class EmailConfirmationDto : BaseUserDto
    {
        public string EmailConfirmationToken { get; set; }
    }

    public class UserUpdateDTo
    {
        [AllowNull]
        public IFormFile? ProfilePicture { get; set; }
        public string? DisplayName { get; set; }
        public string? Gender { get; set; }
    }
}
