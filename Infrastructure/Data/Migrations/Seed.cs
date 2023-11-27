using Core.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Migrations
{
    public class Seed
    {
        public static async Task SeedUsersAsync(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
        {
            if (await userManager.Users.AnyAsync())
            {
                return;
            }

            var user = new AppUser
            {
                DisplayName = "Hamza",
                Email = "benmail332@gmail.com",
                UserName = "BenMail@332",
                Address = new UserAddress
                {
                    Street = "10 The street",
                    City = "Lahore",
                    Country = "Pakistan"
                }
            };

            await userManager.CreateAsync(user, "Pa$$w0rd");
            await userManager.AddToRoleAsync(user, "Administrator");

        }
    }
}
