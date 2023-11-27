using Core.Entities.Identity;

namespace Core.Interfaces
{
    public interface ITokenServices
    {
        Task<string> CreateToken(AppUser user);
        public string GetEmailFromJwtToken(string token);
    }
}
