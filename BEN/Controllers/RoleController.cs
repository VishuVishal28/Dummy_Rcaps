using Core.Entities.Identity;
using DataTransferObjects.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BEN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly RoleManager<AppRole> _roleManager;
        private readonly UserManager<AppUser> _userManager;

        public RoleController(RoleManager<AppRole> roleManager, UserManager<AppUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] string roleName)
        {
            if (!string.IsNullOrEmpty(roleName))
            {
                var roleExists = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    var role = new AppRole { Name = roleName };
                    await _roleManager.CreateAsync(role);
                    return Ok("Role created successfully.");
                }
                else
                {
                    return BadRequest("Role already exists.");
                }
            }
            else
            {
                return BadRequest("Role name cannot be empty.");
            }
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignRoleToUser([FromBody] RoleAssignmentDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid model.");
            }

            var role = await _roleManager.FindByNameAsync(model.RoleName);
            if (role == null)
            {
                return BadRequest("Role not found.");
            }

            var user = await _userManager.FindByIdAsync(model.UserId.ToString());
            if (user == null)
            {
                return BadRequest("User not found.");
            }

            var userRoleExists = await _userManager.IsInRoleAsync(user, role.Name);
            if (userRoleExists)
            {
                return BadRequest("User already has the role.");
            }

            var result = await _userManager.AddToRoleAsync(user, role.Name);
            if (result.Succeeded)
            {
                return Ok("Role assigned to the user successfully.");
            }
            else
            {
                return BadRequest("Failed to assign role to the user.");
            }
        }
    }
}
