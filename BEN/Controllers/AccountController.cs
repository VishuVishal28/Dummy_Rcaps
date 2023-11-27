using AutoMapper;
using BEN.DTOs;
using BEN.Errors;
using BEN.Extensions;
using Core.Entities.Identity;
using Core.Interfaces;
using Core.Models;
using DataTransferObjects.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BEN.Controllers
{
    public class TokenVerifier
    {
        public string Token { get; set; }

        public string jwt { get; set; }

    }
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly ITokenServices _tokenServices;
        private readonly IMapper _mapper;
        private readonly IMailService _mailService;

        public AccountController(UserManager<AppUser> userManager,
                                 SignInManager<AppUser> signInManager,
                                 RoleManager<AppRole> roleManager,
                                 ITokenServices tokenServices,
                                 IMapper mapper,
                                 IMailService mailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenServices = tokenServices;
            _mailService = mailService;
            _mapper = mapper;
            _roleManager = roleManager;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var user = await _userManager.FindByEmailFromClaimsPrinciple(HttpContext.User);
            return new UserDto
            {
                Email = user.Email,
                Token = await _tokenServices.CreateToken(user),
                DisplayName = user.DisplayName,
            };
        }

        [HttpGet("emailexists")]
        public async Task<ActionResult<bool>> CheckEmailExistsAsync([FromQuery] string email)
        {
            var isExist = await _userManager.FindByEmailAsync(email) != null;
            return isExist;
        }

        [HttpGet("usernameexists")]
        public async Task<ActionResult<bool>> CheckUserNameExistsAsync([FromQuery] string userName)
        {
            var isExist = await _userManager.FindByNameAsync(userName) != null;
            return isExist;
        }

        [Authorize]
        [HttpGet("address")]
        public async Task<ActionResult<AddressDto>> GetUserAddress()
        {
            var user = await _userManager.FindByEmailWithAddressAsync(HttpContext.User);

            return _mapper.Map<UserAddress, AddressDto>(user.Address);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = new AppUser();

            if (!string.IsNullOrWhiteSpace(loginDto.UserName))
            {
                user = await _userManager.FindByNameAsync(loginDto.UserName);
            }
            else
            {
                user = await _userManager.FindByEmailAsync(loginDto.Email);
            }

            if (user == null)
            {
                return Unauthorized(new ApiResponse(401));
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized(new ApiResponse(401));
            }

            return new UserDto
            {
                Email = user.Email,
                UserName = user.UserName,
                Token = await _tokenServices.CreateToken(user),
                DisplayName = user.DisplayName,
                ProfileInfo = new UserProfileInfo ()
                {
                    DisplayName = user.DisplayName,
                    Gender = user.Gender,
                    ProfilePicturUrl  =  user.ProfilPicture
                }

            };
        }

        [HttpPost("login-otp")]
        public async Task<ActionResult<UserDto>> LoginWithOtp(string otp, string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            var signIn = await _signInManager.TwoFactorSignInAsync("Email", otp, false, false);
            if (signIn.Succeeded)
            {
                if (user != null)
                {
                    return new UserDto
                    {
                        Email = user.Email,
                        Token = await _tokenServices.CreateToken(user),
                        DisplayName = user.DisplayName,
                    };
                }
            }

            return NotFound(new ApiResponse(404));
        }

        [Authorize]
        [HttpPut("address")]
        public async Task<ActionResult<AddressDto>> UpdateUserAddress(AddressDto address)
        {
            var user = await _userManager.FindByEmailWithAddressAsync(User);

            user.Address = _mapper.Map<UserAddress>(address);

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok(_mapper.Map<AddressDto>(user.Address));
            }

            return BadRequest("Problem updating the user");
        }

        [HttpPost("register")]
        public async Task<ActionResult<EmailConfirmationDto>> Register(RegisterDto registerDto)
        {

            try
            {
                if (CheckEmailExistsAsync(registerDto.Email).Result.Value)
                {
                    return new BadRequestObjectResult(new ApiValidationErrorResponse
                    {
                        Errors = new[] { "Email is already exist." }
                    });
                }
                if (CheckUserNameExistsAsync(registerDto.Username).Result.Value)
                {
                    return new BadRequestObjectResult(new ApiValidationErrorResponse
                    {
                        Errors = new[] { "User Name is already exist." }
                    });
                }
                var user = _mapper.Map<AppUser>(registerDto);
                user.Gender = "Male";
                user.TwoFactorEnabled = false;
                user.ProfilPicture = "";
             
                var result = await _userManager.CreateAsync(user, registerDto.Password);

                if (!result.Succeeded)
                {
                    return BadRequest(new ApiResponse(400));
                }

                var Roles = new RoleController(_roleManager, _userManager);
                await Roles.AssignRoleToUser(new RoleAssignmentDto()
                {
                    RoleName = registerDto.Role,
                    UserId = user.Id
                });
                var token = await SendEmailToUserAsync(user, registerDto.Link);
                return new EmailConfirmationDto
                {
                    Email = user.Email,
                    UserName = user.UserName,
                    EmailConfirmationToken = token,
                    DisplayName = user.DisplayName,
                };
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(new ApiValidationErrorResponse
                {
                    Errors = new[] { e.Message }
                });
            }

        }


        [HttpPost("verifyUser")]
        public async Task<ActionResult<UserDto>> VerifyUser([FromBody] TokenVerifier tokenVerifier)
        {
            var jwt = tokenVerifier.jwt;
            var Token = tokenVerifier.Token;

            var email = _tokenServices.GetEmailFromJwtToken(jwt);
            if (email == null)
            {
                return new BadRequestObjectResult(new ApiValidationErrorResponse
                {
                    Errors = new[] { "Wronge Email" }
                });
            }
            if (!CheckEmailExistsAsync(email).Result.Value)
            {
                return new BadRequestObjectResult(new ApiValidationErrorResponse
                {
                    Errors = new[] { "Wronge Email" }
                });
            }
            var user = _userManager.FindByEmailAsync(email).Result;


            var result = await _userManager.ConfirmEmailAsync(user, Token);
            if (result.Succeeded)
            {
                return new UserDto
                {
                    Email = user.UserName,
                    Token = await _tokenServices.CreateToken(user),
                    DisplayName = user.DisplayName,
                };
            }
            return BadRequest(new ApiResponse(404));

        }
        private async Task<string> SendEmailToUserAsync(AppUser user, string link)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var jwtToken = await _tokenServices.CreateToken(user);
            await _mailService.SendEmailAsync(new MailRequest()
            {
                Subject = "",
                ToEmail = user.Email,
                Body = "An EMAIL FOR REGISTRATION HAS BEEN SENT, PLEASE OPEN LINK TO VERIFY: " + link + $"?token={token}&email={jwtToken}"

            });
            return token;
        }
        [HttpPost("resend")]
        public async Task<ActionResult<EmailConfirmationDto>> Resend([FromQuery] string link, string Email)
        {

            if (!CheckEmailExistsAsync(Email).Result.Value)
            {
                return new BadRequestObjectResult(new ApiValidationErrorResponse
                {
                    Errors = new[] { "Wronge Email" }
                });
            }
            var user = _userManager.FindByEmailAsync(Email).Result;
            var token = await SendEmailToUserAsync(user, link);

            return new EmailConfirmationDto
            {
                Email = user.UserName,
                EmailConfirmationToken = token,
                DisplayName = user.DisplayName,
            };

            return BadRequest(new ApiResponse(404));

        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<ActionResult> ChangePassword(ChangePasswordDto changePasswordDto)
        {
            var user = await _userManager.FindByEmailFromClaimsPrinciple(HttpContext.User);

            var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);

            if (result.Succeeded)
            {
                return Ok();
            }

            return BadRequest("Failed to change password.");
        }

        [Authorize]
        [HttpPost("update-profile")]
        public async Task<ActionResult> UpdateProfile([FromForm] UserUpdateDTo userUpdateDto)
        {
            var user = await _userManager.FindByEmailFromClaimsPrinciple(HttpContext.User);

            if (userUpdateDto.ProfilePicture != null)
            {

                if (!IsImageFileValid(userUpdateDto?.ProfilePicture))
                {
                    return BadRequest("Invalid profile picture file.");
                }

                string profilePictureUrl = await UploadProfilePicture(userUpdateDto.ProfilePicture);
                user.ProfilPicture = profilePictureUrl;
            }




               
                user.DisplayName = userUpdateDto.DisplayName;
                user.Gender = "mm";

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    user = await _userManager.FindByEmailFromClaimsPrinciple(HttpContext.User);
                    return Ok(new UserProfileInfo()
                    {
                        DisplayName = userUpdateDto.DisplayName,
                        Gender = userUpdateDto.Gender,
                        ProfilePicturUrl = user.ProfilPicture
                    });
                }

                return BadRequest("Failed to update profile picture.");
        }

        private bool IsImageFileValid(IFormFile file)
        {
            // Check file type and size
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var maxFileSize = 5 * 1024 * 1024; // 5MB

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Contains(fileExtension) && file.Length <= maxFileSize;
        }

        private async Task<string> UploadProfilePicture(IFormFile file)
        {
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string uploadPath = Path.Combine("wwwroot", "Profile");
            string filePath = Path.Combine(uploadPath, uniqueFileName);

            Directory.CreateDirectory(uploadPath);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/Profile/{uniqueFileName}";
        }



    }
}
