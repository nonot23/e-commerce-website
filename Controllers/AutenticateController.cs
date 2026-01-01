using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using Microsoft.IdentityModel.Tokens;
using StoreAPI.Models;

namespace StoreAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AutenticateController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration; // ใช้กับพวก jwt
    public AutenticateController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    //Resgister for normal user
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModels model)
    {
        //เช็คว่ามี user นี้อยู่หรือไม่
        var userExist = await _userManager.FindByNameAsync(model.Username);
        if (userExist != null)
        {
            //แสดงว่ามี user นี้อยู่แล้ว
            return StatusCode(StatusCodes.Status500InternalServerError,
            new Response
            {
                Status = "Error",
                Message = "User already exists!"
            });
        }

        IdentityUser user = new() // ถ้าไม่มีสร้าง user ใหม่
        {
            Email = model.Email,
            SecurityStamp = Guid.NewGuid().ToString(), //Generate id random
            UserName = model.Username
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        // ถ้าสร้าง user ไม่สําเร็จ
        if (!result.Succeeded)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
            new Response
            {
                Status = "Error",
                Message = "User creation failed! Please check user details and try again."
            });
        }
        else
        {
            // ถ้าสร้างสำเร็จ
            return Ok(new Response
            {
                Status = "Success",
                Message = "User created successfully!"
            });
        }
    }

    [HttpPost("register-admin")]
    public async Task<IActionResult> RegisterAdmin([FromBody] RegisterModels model)
    {
        //เช็คว่ามี user นี้อยู่หรือไม่
        var userExist = await _userManager.FindByNameAsync(model.Username!);
        if (userExist != null)
        {
            //แสดงว่ามี user นี้อยู่แล้ว
            return StatusCode(StatusCodes.Status500InternalServerError,
            new Response
            {
                Status = "Error",
                Message = "User already exists!"
            });
        }

        IdentityUser user = new() // ถ้าไม่มีสร้าง user ใหม่
        {
            Email = model.Email,
            SecurityStamp = Guid.NewGuid().ToString(), //Generate id random
            UserName = model.Username
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        // ถ้าสร้าง user ไม่สําเร็จ
        if (!result.Succeeded)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
            new Response
            {
                Status = "Error",
                Message = "User creation failed! Please check user details and try again."
            });
        }
        if (!await _roleManager.RoleExistsAsync(UserRoles.Admin))
            await _roleManager.CreateAsync(new IdentityRole(UserRoles.Admin));
        if (!await _roleManager.RoleExistsAsync(UserRoles.Manager))
            await _roleManager.CreateAsync(new IdentityRole(UserRoles.Manager));
        if (!await _roleManager.RoleExistsAsync(UserRoles.User))
            await _roleManager.CreateAsync(new IdentityRole(UserRoles.User));

        return Ok(new Response
        {
            Status = "Success",
            Message = "User created successfully!"
        });

        
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] LoginModels model)
    {
        var user = await _userManager.FindByNameAsync(model.Username);

        //ถ้า login สำเร็จ
        if(user != null && await _userManager.CheckPasswordAsync(user, model.Password!))
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var token = GetToken(authClaims);

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token), 
                expiration = token.ValidTo
            });
        }
        else
        {
            return Unauthorized();
        }
    }

    //Method for generating JWT Token
    private JwtSecurityToken GetToken(List<Claim> authClaims)
    {
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]!));
        var token = new JwtSecurityToken(
            issuer: _configuration["JWT:ValidIssuer"], //ออกโดยใคร  
            audience: _configuration["JWT:ValidAudience"], //ออกให้ใคร (ผู้ใช้งาน)
            expires: DateTime.Now.AddHours(3),
            claims: authClaims, 
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );
        return token;
        
    }
}


