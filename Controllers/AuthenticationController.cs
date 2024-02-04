
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using pidepeapi.Dtos;
using pidepeapi.Models;

namespace pidepeapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
    public class AuthenticationController : ControllerBase
    {

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IConfiguration _configuration;
        public AuthenticationController(UserManager<ApplicationUser> userManager, IConfiguration configuration, RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        [HttpPost]
        [Route("createrole")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
        {
            try{
                var appRole = new ApplicationRole {
                    Name = request.Role
                };

                var result = await _roleManager.CreateAsync(appRole);
                return result.Succeeded ? Ok(result) : BadRequest(result.Errors);
            } catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var result = await RegisterAsync(request);
                return result.Success ? Ok(result) : BadRequest(result.Message); 
            } catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private async Task<RegisterResponse> RegisterAsync(RegisterRequest registerRequest){
            try
            {
                var userExists = await _userManager.FindByEmailAsync(registerRequest.Email);
                if (userExists != null) return new RegisterResponse { Success = false, Message = "User already exists." };

                userExists = new ApplicationUser{
                    FullName = registerRequest.FullName,
                    Email = registerRequest.Email,
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    UserName = registerRequest.UserName,
                    PhoneNumber = registerRequest.PhoneNumber,
                };

                var createUserResult = await _userManager.CreateAsync(userExists, registerRequest.Password);
                if (!createUserResult.Succeeded) return new RegisterResponse { Success = false, Message = $"User creation failed. {createUserResult.Errors.First().Description}"};

                var addUserToRole = await _userManager.AddToRoleAsync(userExists, "USER");
                if (!addUserToRole.Succeeded) return new RegisterResponse { Success = false, Message = $"User creation success but role assignment failed. {addUserToRole.Errors.First().Description}"};

                return new RegisterResponse { Success = true, Message = "User created successfully." };
            } 
            catch (Exception ex)
            {
                return new RegisterResponse { Success = false, Message = ex.Message };
            }
        }

        [HttpPost]
        [Route("login")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(LoginResponse))]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await LoginAsync(request);
                return result.Success ? Ok(result) : BadRequest(result.Message); 
            } catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private async Task<LoginResponse> LoginAsync(LoginRequest loginRequest){

            try
            {
                var user = await _userManager.FindByEmailAsync(loginRequest.Email);
                if (user == null) return new LoginResponse { Success = false, Message = "Invalid Email." };

                var result = await _userManager.CheckPasswordAsync(user, loginRequest.Password);
                if (!result) return new LoginResponse { Success = false, Message = "Invalid Password." };

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName!),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())  
                };
                var roles = await _userManager.GetRolesAsync(user);
                var roleClaims = roles.Select(x => new Claim(ClaimTypes.Role, x));

                claims.AddRange(roleClaims);

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("Jwt:Key").Value!));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var expires = DateTime.Now.AddDays(365);

                var token = new JwtSecurityToken(
                    issuer: _configuration.GetSection("Jwt:Issuer").Value,
                    audience: _configuration.GetSection("Jwt:Audience").Value,
                    claims: claims,
                    expires: expires,
                    signingCredentials: creds
                );

                return new LoginResponse{
                    Success = true,
                    AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                    Message = "Login Successful.",
                    Email = user.Email!,
                    UserID = user.Id.ToString(),
                };
            } catch (Exception ex)
            {
                return new LoginResponse { Success = false, Message = ex.Message };
            }
        }
    }
}