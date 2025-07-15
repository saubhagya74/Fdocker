using app.Data;
using app.DTO;
using app.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using static app.DTO.RefreshTokenRequest;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace app.Controllers
{

        [Route("api/[controller]")]
        [ApiController]
        public class AuthController : ControllerBase
        {
            private readonly ApplicationDbContext _context;
            private readonly IConfiguration _configuration;
            private readonly SnowFlakeGen _idGen;

        public AuthController(ApplicationDbContext context, IConfiguration configuration, SnowFlakeGen idGen)
        {
            _context = context;
            _configuration = configuration;
            _idGen = idGen;
        }

           [HttpGet]
           public IActionResult Get() => Ok("API is running");

        [HttpPost("register")]
            public async Task<ActionResult<object>> Register(UserDto request)
            {
                if (await _context.Users.AnyAsync(u => u.UserName == request.UserName))
                    return BadRequest("User already exists.");

            var user = new UserEntity
            {
                UserId = _idGen.GenerateId(),
                UserName = request.UserName,
                PasswordHash = new PasswordHasher<UserEntity>().HashPassword(null, request.Password),//the place where null is sitting , can be used to put other entities to salt the hash
                Role = string.Empty,
                NumOfFriends = 0
            };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return Ok(new { user.UserId, user.UserName });
            }

            [HttpPost("login")]
            public async Task<ActionResult<TokenResponseDto>> Login(UserDto request)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == request.UserName);
                if (user == null)
                    return BadRequest("User or password is invalid.");

                var result = new PasswordHasher<UserEntity>().VerifyHashedPassword(user, user.PasswordHash, request.Password);
                if (result == PasswordVerificationResult.Failed)
                    return BadRequest("User or password is invalid.");
                //if pass is correct return refresh token and access token
                return await CreateTokenResponse(user);
            }
            //when this is invoked
            [HttpPost("refresh")]
            [Authorize]
            public async Task<ActionResult<TokenResponseDto>> Refresh([FromBody] RefreshTokenRequestDto request)
            {
                var user = await ValidateRefreshTokenAsync(request.UserId, request.RefreshToken);
                if (user == null)
                    return Unauthorized("Invalid refresh token");

                return await CreateTokenResponse(user);
            }

            private async Task<UserEntity?> ValidateRefreshTokenAsync(long userId, string refreshToken)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                {
                    return null;
                }
                //if valid return user entity 
                return user;
            }

            private async Task<TokenResponseDto> CreateTokenResponse(UserEntity user)
            {
                var token = CreateToken(user);
                var response = new TokenResponseDto
                {
                    AccessToken = token,
                    RefreshToken = await GenerateAndSaveRefereshTokenAsync(user)
                };
                return response;
            }
            private string CreateToken(UserEntity user)
            {
                var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.UserName),
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Role, user.Role)
            };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                    _configuration.GetValue<string>("AppSettings:Token")!));

                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

                var token = new JwtSecurityToken(
                    issuer: _configuration.GetValue<string>("AppSettings:Issuer"),
                    audience: _configuration.GetValue<string>("AppSettings:Audience"),
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(10), // shorter expiry = better token rotation
                    signingCredentials: credentials
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }

            private async Task<string> GenerateAndSaveRefereshTokenAsync(UserEntity user)
            {
                var RNG = new byte[32];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(RNG);
                var refereshToken = Convert.ToBase64String(RNG);
                user.RefreshToken = refereshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await _context.SaveChangesAsync();
                return refereshToken;
            }

        }
    }

