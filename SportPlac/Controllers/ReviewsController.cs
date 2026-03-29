using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportPlac.Data;
using SportPlac.Models;
using SportPlac.Models.DTOs;

namespace SportPlac.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class ReviewsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReviewsController(AppDbContext context)
        {
            _context = context;
        }
        [AllowAnonymous]
        [HttpGet("stores/{id}/reviews")]
        public async Task<IActionResult> GetStoreReviews(Guid id, int page = 1, int pageSize = 10)
        {
            var reviews = await _context.Reviews
                .AsNoTracking()
                .Where(r => r.StoreId == id)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    r.Id,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt,

                    Reviewer = new
                    {
                        r.Reviewer.Id,
                        r.Reviewer.FirstName,
                        r.Reviewer.LastName,
                        r.Reviewer.ProfileImageUrl
                    }
                })
                .ToListAsync();

            return Ok(reviews);
        }

        [HttpPost("stores/{id}/reviews")]
        public async Task<IActionResult> CreateReview(Guid id, CreateReviewDto dto)
        {
            var userId = Guid.Parse(User.FindFirst("sub")!.Value);

            // validacija rating
            if (dto.Rating < 1 || dto.Rating > 5)
                return BadRequest("Rating must be between 1 and 5");

            var store = await _context.Stores
                .Select(s => new { s.Id, s.UserId })
                .FirstOrDefaultAsync(s => s.Id == id);

            if (store == null)
                return NotFound("Store not found");

            // ❌ ne možeš oceniti sebe
            if (store.UserId == userId)
                return BadRequest("You cannot review your own store");

            // ❌ jedan review po useru
            var exists = await _context.Reviews
                .AnyAsync(r => r.StoreId == id && r.ReviewerId == userId);

            if (exists)
                return BadRequest("You already reviewed this store");

            var review = new Review
            {
                Id = Guid.NewGuid(),
                StoreId = id,
                ReviewerId = userId,
                SellerId = store.UserId,
                Rating = dto.Rating,
                Comment = dto.Comment
            };

            _context.Reviews.Add(review);

            // 🔥 update store rating (ResponseRate možeš koristiti kao avg rating)
            var ratings = await _context.Reviews
                .Where(r => r.StoreId == id)
                .Select(r => r.Rating)
                .ToListAsync();

            ratings.Add(dto.Rating);

            var avg = ratings.Average();

            var storeEntity = await _context.Stores.FindAsync(id);
            storeEntity.ResponseRate = avg;

            await _context.SaveChangesAsync();

            return Ok(review.Id);
        }
        [HttpDelete("reviews/{id}")]
        public async Task<IActionResult> DeleteReview(Guid id)
        {
            var userId = Guid.Parse(User.FindFirst("sub")!.Value);

            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null)
                return NotFound();

        
            if (review.ReviewerId != userId && !User.IsInRole("Admin"))
                return Forbid();

            _context.Reviews.Remove(review);

            await _context.SaveChangesAsync();

            return Ok();
        }

    }
}
