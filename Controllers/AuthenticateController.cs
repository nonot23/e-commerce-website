using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using StoreAPI.Models;

namespace StoreAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthenticateController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration; // ใช้กับพวก jwt
    public AuthenticateController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration; // JWT
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
        // สร้าง role "User" ถ้ายังไม่มี
        if (!await _roleManager.RoleExistsAsync(UserRoles.User))
        {
            await _roleManager.CreateAsync(new IdentityRole(UserRoles.User));
        }
        await _userManager.AddToRoleAsync(user, UserRoles.User);




        return Ok(new Response
        {
            Status = "Success",
            Message = "User created successfully!"
        });
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
        Console.WriteLine(userExist);

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
        else if (!await _roleManager.RoleExistsAsync(UserRoles.Manager))
            await _roleManager.CreateAsync(new IdentityRole(UserRoles.Manager));
        else if (!await _roleManager.RoleExistsAsync(UserRoles.User))
            await _roleManager.CreateAsync(new IdentityRole(UserRoles.User));

        // สร้าง role "Admin" ถ้ายังไม่มี
        else if (!await _roleManager.RoleExistsAsync(UserRoles.Admin))
        {
            await _roleManager.CreateAsync(new IdentityRole(UserRoles.Admin));
        }
        await _userManager.AddToRoleAsync(user, UserRoles.Admin);


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
        if (user != null && await _userManager.CheckPasswordAsync(user, model.Password!))
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

    //refresh token
    [HttpPost("refresh-token")]
    public IActionResult RefreshToken([FromBody] RefreshTokenModels model)
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        if (authHeader != null && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.ToString().Substring("Bearer ".Length).Trim();
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]!);
            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var user = new
                {
                    Name = jwtToken.Claims.First(x => x.Type == ClaimTypes.Name).Value,
                    Role = jwtToken.Claims.First(x => x.Type == ClaimTypes.Role).Value
                };

                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var newToken = GetToken(authClaims);
                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(newToken),
                    expiration = newToken.ValidTo
                });
            }
            catch
            {
                return Unauthorized();
            }
        }

        return Unauthorized();

    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        // 1. ดึงชื่อ User จาก Claims (Token ที่แนบมาใน Header)
        var username = User.Identity?.Name;

        if (username == null)
        {
            return Unauthorized();
        }

        // 2. ค้นหา User ในฐานข้อมูล
        var user = await _userManager.FindByNameAsync(username);

        if (user == null)
        {
            return BadRequest("Invalid user");
        }

        // 3. ทำลาย Refresh Token (ตัวอย่างถ้าคุณเก็บไว้ใน Column ชื่อ RefreshToken)
        // user.RefreshToken = null;
        // user.RefreshTokenExpiryTime = null;

        
        await _userManager.RemoveAuthenticationTokenAsync(user, "JWT", "RefreshToken");

        // 4. บันทึกการเปลี่ยนแปลง
        await _userManager.UpdateAsync(user);

        return Ok(new Response
        {
            Status = "Success",
            Message = "User logged out successfully!"
        });
    }

    //Method for generating JWT Token
    private JwtSecurityToken GetToken(List<Claim> authClaims)
    {
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]!));
        var token = new JwtSecurityToken(
            issuer: _configuration["JWT:ValidIssuer"], //ออกโดยใคร  
            audience: _configuration["JWT:ValidAudience"], //ออกให้ใคร (ผู้ใช้งาน)
            expires: DateTime.UtcNow.AddDays(1),
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );
        Console.WriteLine("Issuer: " + _configuration["JWT:ValidIssuer"]);
        Console.WriteLine("Audience: " + _configuration["JWT:ValidAudience"]);

        return token;

    }

    public class RefreshTokenModels
    {
        public required string Token { get; set; }
    }
}


