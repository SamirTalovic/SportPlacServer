using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportPlac.Data;
using SportPlac.Models;
using SportPlac.Models.DTOs;
using SportPlac.Services;
using System.Security.Claims;

namespace SportPlac.Controllers
{
    [ApiController]
    [Route("api/stores")]
    public class StoresController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly CloudinaryService _cloudinary;

        public StoresController(AppDbContext context, CloudinaryService cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }


        [HttpGet]
        public async Task<IActionResult> GetStores(int page = 1, int pageSize = 10)
        {
            var stores = await _context.Stores
                .AsNoTracking()
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new StoreDTO
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    Location = s.Location,
                    AvatarUrl = s.AvatarUrl,
                    IsVerified = s.IsVerified,
                    TotalSales = s.TotalSales,
                    ResponseRate = s.ResponseRate,
                    ListingsCount = s.Listings.Count,
                    ReviewsCount = s.Reviews.Count
                })
                .ToListAsync();

            return Ok(stores);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetStore(Guid id)
        {
            var store = await _context.Stores
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Description,
                    s.Location,
                    s.AvatarUrl,
                    s.IsVerified,
                    s.TotalSales,
                    s.ResponseRate,

                    Listings = s.Listings.Select(l => new
                    {
                        l.Id,
                        l.Title,
                        l.Price
                    }),

                    Reviews = s.Reviews.Select(r => new
                    {
                        r.Id,
                        r.Rating,
                        r.Comment
                    })
                })
                .FirstOrDefaultAsync();

            if (store == null) return NotFound();

            return Ok(store);
        }
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyStore()
        {
            try 
            {
                var userIdClaim = User.FindFirst("sub")?.Value 
                               ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                               ?? User.FindFirst("id")?.Value;

                if (string.IsNullOrEmpty(userIdClaim)) 
                    return Unauthorized("User ID claim not found in token. Please log out and log in again.");

                if (!Guid.TryParse(userIdClaim, out Guid userId))
                    return BadRequest($"Invalid User ID format in token: {userIdClaim}");

                var store = await _context.Stores
                    .Where(s => s.UserId == userId)
                    .Select(s => new StoreDTO
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Description = s.Description ?? "",
                        Location = s.Location ?? "",
                        AvatarUrl = s.AvatarUrl,
                        IsVerified = s.IsVerified,
                        TotalSales = s.TotalSales,
                        ResponseRate = s.ResponseRate,
                        ListingsCount = s.Listings != null ? s.Listings.Count : 0,
                        ReviewsCount = s.Reviews != null ? s.Reviews.Count : 0
                    })
                    .FirstOrDefaultAsync();

                return Ok(store);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdateStore(UpdateStoreDto dto)
        {
            var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized("User ID not found in token");
            var userId = Guid.Parse(userIdClaim);

            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (store == null) return NotFound();

            store.Name = dto.Name;
            store.Description = dto.Description;
            store.Location = dto.Location;

            await _context.SaveChangesAsync();

            return Ok(store);
        }

        [Authorize]
        [HttpPost("avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized("User ID not found in token");
            var userId = Guid.Parse(userIdClaim);

            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (store == null) return NotFound();

            var url = await _cloudinary.UploadImageAsync(file);

            store.AvatarUrl = url;

            await _context.SaveChangesAsync();

            return Ok(new { url });
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStore(Guid id)
        {
            var store = await _context.Stores
                .Include(s => s.Listings)
                .Include(s => s.Reviews)
                .Include(s => s.Likes)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (store == null) return NotFound();

            _context.Reviews.RemoveRange(store.Reviews);
            _context.Likes.RemoveRange(store.Likes);
            _context.Listings.RemoveRange(store.Listings);

            _context.Stores.Remove(store);

            await _context.SaveChangesAsync();

            return Ok("Store deleted");
        }
        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/verify")]
        public async Task<IActionResult> VerifyStore(Guid id)
        {
            var store = await _context.Stores.FindAsync(id);

            if (store == null) return NotFound();

            store.IsVerified = true;

            await _context.SaveChangesAsync();

            return Ok(store);
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        public async Task<IActionResult> GetAllStores(bool? verified)
        {
            var query = _context.Stores.AsQueryable();

            if (verified.HasValue)
                query = query.Where(x => x.IsVerified == verified);

            var result = await query
                .Select(s => new StoreDTO
                {
                    Id = s.Id,
                    Name = s.Name,
                    IsVerified = s.IsVerified
                })
                .ToListAsync();

            return Ok(result);
        }
        [Authorize]
        [HttpPost("{id}/like")]
        public async Task<IActionResult> LikeStore(Guid id)
        {
            var userId = Guid.Parse(User.FindFirst("sub")!.Value);

            var store = await _context.Stores
                .Where(s => s.Id == id)
                .Select(s => new { s.Id, s.UserId, s.Name })
                .FirstOrDefaultAsync();

            if (store == null)
                return NotFound("Store not found");


            if (store.UserId == userId)
                return BadRequest("You cannot like your own store");

           
            var exists = await _context.Likes
                .AnyAsync(l => l.UserId == userId && l.StoreId == id);

            if (exists)
                return BadRequest("Already liked");

            var like = new Like
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                StoreId = id
            };

            _context.Likes.Add(like);

          
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = store.UserId, 
                Type = NotificationType.Like,
                Title = "Novi lajk",
                Message = $"Neko je lajkovao tvoj store: {store.Name}",
                ReferenceId = id.ToString()
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Store liked" });
        }

        [Authorize]
        [HttpDelete("{id}/like")]
        public async Task<IActionResult> UnlikeStore(Guid id)
        {
            var userId = Guid.Parse(User.FindFirst("sub")!.Value);

            var like = await _context.Likes
                .FirstOrDefaultAsync(l => l.UserId == userId && l.StoreId == id);

            if (like == null)
                return NotFound("Like not found");

            _context.Likes.Remove(like);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Like removed" });
        }


    }


}