using Core.Entities.Identity;
using Core.Interfaces;
using Core.Models;
using DataTransferObjects.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace BEN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ForgotPasswordController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IMailService _emailService;
        private readonly ILogger<ForgotPasswordController> _logger;
        private readonly ITokenServices _tokenServices;
        public ForgotPasswordController(UserManager<AppUser> userManager, IMailService emailService, ILogger<ForgotPasswordController> logger, ITokenServices tokenServices)
        {
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
            _tokenServices = tokenServices;
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest();
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                    return BadRequest("Invalid Request");
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var param = new Dictionary<string, string?>
                {
                 {"token", token },
                 {"email", await _tokenServices.CreateToken(user)}
                };

                var callback = QueryHelpers.AddQueryString(model.ClientURI, param);
                var message = new MailRequest()
                {
                    ToEmail = model.Email,
                    Subject = "Reset Password",
                    Body = callback
                };

                await _emailService.SendEmailAsync(message);

                return Ok();
            }
            catch(Exception ex)
            {
                return Ok();
            }
        }
        [HttpPost("NewPassword")]
        public async Task<IActionResult> NewPassword([FromBody] NewPasswordRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid request. Email, code, and new password are required.");
            }
            model.Email = _tokenServices.GetEmailFromJwtToken(model.Email);
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.NewPassword);
            if (result.Succeeded)
            {
                // Password reset successful. You can return a success message or perform additional actions if needed.
                return Ok("Password reset successful.");
            }
            else
            {
                // Password reset failed. You can handle the failure based on the result.Errors returned by Identity.
                return BadRequest("Password reset failed. Please try again.");
            }
        }

    }
}