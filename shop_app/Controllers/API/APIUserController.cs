using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using shop_app.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace shop_app.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class APIUserController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        public APIUserController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }
        //Register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(new { message = "The model is invalid ..." });
            }
            var newUser = new IdentityUser
            {
                Email = model.Email,
                UserName = model.Email,
                EmailConfirmed = true //Imitation check email
            };
            var result = await _userManager.CreateAsync(newUser, model.Password);
            if (result.Succeeded)
            {
                return Ok(new { message = "Registered successfully ..." });
            }
            return BadRequest(result.Errors);
        }
        [HttpPost("auth")]
        public async Task<IActionResult> Auth([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "The model is invalid ..." });
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if(user == null)
            {
                return NotFound();
            }
            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (result.Succeeded)
            {
                var token = GenerateJwtToken(user);
                return Ok(new { Token = token});
            }

            return BadRequest(new { message = "Invalid email or password ..." });
        }
        // Генерация JWT-токена для пользователя
        private string GenerateJwtToken(IdentityUser user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();  // Создание обработчика для токенов
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);  // Получение ключа из конфигурации и его кодирование в байты

            var tokenDescriptor = new SecurityTokenDescriptor  // Определение параметров токена (описание токена)
            {
                Subject = new ClaimsIdentity(new[]  // Добавление утверждений (claims) о пользователе в токен
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),  // Утверждение: идентификатор пользователя
                    new Claim(ClaimTypes.Name, user.UserName),  // Утверждение: имя пользователя
                    new Claim(ClaimTypes.Email, user.Email)  // Утверждение: email пользователя
                }),
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:DurationInMinutes"])),  // Установка срока действия токена
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256),  // Подпись токена с использованием симметричного ключа и алгоритма HMAC SHA256 (потрібен пароль у appsettings.json мінімум 32 символи)
                Issuer = _configuration["Jwt:Issuer"],  // Установка издателя токена (опционально)
                Audience = _configuration["Jwt:Audience"]  // Установка аудитории токена (опционально)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);  // Создание токена на основе описания
            return tokenHandler.WriteToken(token);  // Возврат сгенерированного токена в строковом формате
        }
    }
}
