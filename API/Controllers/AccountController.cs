using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        private readonly SignInManager<AppUser> _signInManager;
        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ITokenService tokenService, IMapper mapper)
        {
            _signInManager = signInManager;
            _mapper = mapper;
            _userManager = userManager;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(UserRegister userRegister)
        {
            if (await UserExists(userRegister.UserName))
            {
                return BadRequest("Username is Taken");
            }

            if (userRegister.Password != userRegister.ConfirmPassword)
            {
                return BadRequest("Password and Password Confirmation don't match");
            }

            var user = _mapper.Map<AppUser>(userRegister);

            user.UserName = userRegister.UserName.ToLower();

            var result = await _userManager.CreateAsync(user, userRegister.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "Member");

            if (!roleResult.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return new UserDto
            {
                Username = user.UserName,
                Token = await _tokenService.CreateTokenAsync(user),
                KnownAs = user.KnownAs,
                Gender = user.Gender
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(UserLogin userLogin)
        {
            var user = await _userManager.Users
                .Include(user => user.Photos)
                .SingleOrDefaultAsync(user => user.UserName == userLogin.UserName.ToLower());

            if (user == null)
            {
                return BadRequest("Invalid login/password");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, userLogin.Password, false);

            if (!result.Succeeded)
            {
                return Unauthorized();
            }


            return new UserDto
            {
                Username = user.UserName,
                Token = await _tokenService.CreateTokenAsync(user),
                PhotoUrl = user.Photos.FirstOrDefault(photo => photo.IsMain)?.Url,
                KnownAs = user.KnownAs,
                Gender = user.Gender
            };
        }

        private async Task<bool> UserExists(string username)
        {
            return await _userManager.Users.AnyAsync(user => user.UserName == username.ToLower());
        }
    }
}
