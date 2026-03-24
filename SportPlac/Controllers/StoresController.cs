using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportPlac.Data;
using SportPlac.Models.DTOs;
using SportPlac.Services;

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
            var userId = Guid.Parse(User.FindFirst("sub")!.Value);

            var store = await _context.Stores
                .Where(s => s.UserId == userId)
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
                .FirstOrDefaultAsync();

            return Ok(store);
        }
        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdateStore(UpdateStoreDto dto)
        {
            var userId = Guid.Parse(User.FindFirst("sub")!.Value);

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
            var userId = Guid.Parse(User.FindFirst("sub")!.Value);

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
                .Select(s => new StoreDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    IsVerified = s.IsVerified
                })
                .ToListAsync();

            return Ok(result);
        }



    }


}
    