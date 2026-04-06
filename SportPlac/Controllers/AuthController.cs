using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportPlac.Data;
using SportPlac.Models;
using SportPlac.Models.DTOs;
using SportPlac.Services;
using SportPlac.Services.SportPlac.Services;

namespace SportPlac.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwt;
        private readonly CloudinaryService _cloudinaryService;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly EmailService _emailService;

        public AuthController(AppDbContext context, JwtService jwt, CloudinaryService cloudinaryService, EmailService emailService)
        {
            _context = context;
            _jwt = jwt;
            _passwordHasher = new PasswordHasher<User>();
            _cloudinaryService = cloudinaryService;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(x => x.Email == request.Email))
                return BadRequest("Email already exists");

            string? imageUrl = null;

            if (request.ProfileImage != null)
            {
                imageUrl = await _cloudinaryService.UploadImageAsync(request.ProfileImage);
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Phone = request.Phone ?? "",
                City = request.City ?? "",
                ProfileImageUrl = imageUrl, // ✅ SETUJEMO
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            var store = new Store
            {
                Id = Guid.NewGuid(),
                Name = request.StoreName,
                UserId = user.Id
            };

            _context.Users.Add(user);
            _context.Stores.Add(store);

            await _context.SaveChangesAsync();

            // 📧 SEND WELCOME EMAIL
            try
            {
                var subject = "Dobrodošao na SportPlac 🎉";

                var body = $@"
        <h2>Zdravo {user.FirstName},</h2>

        <p>Uspešno si se registrovao na <b>SportPlac</b> platformu.</p>

        <p>Sada možeš:</p>
        <ul>
            <li>Postavljati oglase</li>
            <li>Komunicirati sa kupcima</li>
            <li>Prodavati svoje proizvode</li>
        </ul>

        <br/>

        <p>Tvoja prodavnica: <b>{store.Name}</b> je već kreirana ✅</p>

        <br/>

        <p>Srećna prodaja! 🚀</p>

        <hr/>
        <small>SportPlac tim</small>
    ";

                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Email error: " + ex.Message);
            }


            var token = _jwt.GenerateToken(user);

            return Ok(new { token, user });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(x => x.Email == request.Email);

            if (user == null)
                return Unauthorized("Invalid credentials");

            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password
            );

            if (result == PasswordVerificationResult.Failed)
                return Unauthorized("Invalid credentials");

            user.LastLoginAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var token = _jwt.GenerateToken(user);

            return Ok(new
            {
                token,
                user
            });
        }
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin(GoogleLoginRequest request)
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken);

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == payload.Email);

            if (user == null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = payload.Email,
                    FirstName = payload.GivenName,
                    LastName = payload.FamilyName,
                    ProfileImageUrl = payload.Picture,
                    AuthProvider = AuthProvider.Google,
                    ExternalAuthId = payload.Subject
                };

                var store = new Store
                {
                    Id = Guid.NewGuid(),
                    Name = payload.GivenName + " Store",
                    UserId = user.Id
                };

                _context.Users.Add(user);
                _context.Stores.Add(store);

                await _context.SaveChangesAsync();
            }

            var token = _jwt.GenerateToken(user);

            return Ok(new
            {
                token,
                user
            });
        }
        [HttpGet]
        public async Task<IActionResult> GetUsers(int page = 1, int pageSize = 10)
        {
            var users = await _context.Users
                .AsNoTracking()
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FirstName + " " + u.LastName,
                    ProfileImageUrl = u.ProfileImageUrl,
                    City = u.City,

                    Store = u.Store != null ? new StoreDTO
                    {
                        Id = u.Store.Id,
                        Name = u.Store.Name
                    } : null,

                    Roles = u.Roles.Select(r => r.Role.ToString()).ToList(),
                    ListingsCount = u.Listings.Count,
                    ReviewsCount = u.ReviewsReceived.Count
                })
                .ToListAsync();

            return Ok(users);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.Phone,
                    u.City,
                    u.ProfileImageUrl,

                    Store = u.Store == null ? null : new
                    {
                        u.Store.Id,
                        u.Store.Name,
                        u.Store.Description,
                        u.Store.TotalSales
                    },

                    Roles = u.Roles.Select(r => r.Role.ToString()),

                    Listings = u.Listings.Select(l => new
                    {
                        l.Id,
                        l.Title,
                        l.Price
                    }),

                    Reviews = u.ReviewsReceived.Select(r => new
                    {
                        r.Id,
                        r.Rating,
                        r.Comment
                    })
                })
                .FirstOrDefaultAsync();

            if (user == null) return NotFound();

            return Ok(user);
        }
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromForm] UpdateUserDto dto, IFormFile? profileImage)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = Guid.Parse(userIdStr);

            // 🔒 user može menjati samo sebe
            if (userId != id)
                return Forbid();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Phone = dto.Phone;
            user.City = dto.City;

            // 🔥 PROFILE IMAGE UPDATE
            if (profileImage != null)
            {
                var url = await _cloudinaryService.UploadImageAsync(profileImage);
                user.ProfileImageUrl = url;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.ProfileImageUrl
            });
        }

    }
}
