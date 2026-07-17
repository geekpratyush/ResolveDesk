using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ResolveDesk.Services.Identity.DTOs;
using ResolveDesk.Services.Identity.Models;
using ResolveDesk.Services.Identity.Services;

namespace ResolveDesk.Services.Identity.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly TokenService _tokenService;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            TokenService tokenService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var allowedRoles = new[] { "Customer", "SupportStaff", "Admin" };
            var role = string.IsNullOrEmpty(dto.Role) ? "Customer" : dto.Role;
            if (!allowedRoles.Contains(role))
            {
                return BadRequest(new AuthResponseDto 
                { 
                    Success = false, 
                    Error = "Invalid role. Must be Customer, SupportStaff, or Admin." 
                });
            }

            var userExists = await _userManager.FindByEmailAsync(dto.Email);
            if (userExists != null)
                return BadRequest(new AuthResponseDto { Success = false, Error = "User already exists." });

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                Provider = "Local"
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new AuthResponseDto { Success = false, Error = errors });
            }

            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }

            await _userManager.AddToRoleAsync(user, role);

            var token = _tokenService.GenerateToken(user, role);

            return Ok(new AuthResponseDto
            {
                Success = true,
                Token = token,
                Email = user.Email,
                FullName = user.FullName,
                Role = role
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Unauthorized(new AuthResponseDto { Success = false, Error = "Invalid credentials." });

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!isPasswordValid)
                return Unauthorized(new AuthResponseDto { Success = false, Error = "Invalid credentials." });

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Customer";

            var token = _tokenService.GenerateToken(user, role);

            return Ok(new AuthResponseDto
            {
                Success = true,
                Token = token,
                Email = user.Email,
                FullName = user.FullName,
                Role = role
            });
        }

        [HttpPost("oauth-login")]
        public async Task<IActionResult> OAuthLogin([FromBody] ExternalLoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = dto.Email;
            var fullName = dto.FullName;

            if (string.IsNullOrEmpty(email))
            {
                email = $"{dto.Provider.ToLower()}_user_{Guid.NewGuid().ToString().Substring(0, 8)}@resolvedesk.com";
            }
            if (string.IsNullOrEmpty(fullName))
            {
                fullName = $"{dto.Provider} User";
            }

            var user = await _userManager.FindByEmailAsync(email);
            var role = "Customer";

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    Provider = dto.Provider
                };

                // Save external user (no password needed as OAuth credentials authenticate them)
                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest(new AuthResponseDto { Success = false, Error = errors });
                }

                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }

                await _userManager.AddToRoleAsync(user, role);
            }
            else
            {
                var roles = await _userManager.GetRolesAsync(user);
                role = roles.FirstOrDefault() ?? "Customer";
            }

            var token = _tokenService.GenerateToken(user, role);

            return Ok(new AuthResponseDto
            {
                Success = true,
                Token = token,
                Email = user.Email,
                FullName = user.FullName,
                Role = role
            });
        }
    }
}
