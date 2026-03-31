using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportPlac.Data;
using SportPlac.Models;
using SportPlac.Models.DTOs;
using SportPlac.Services;

namespace SportPlac.Controllers
{
    [ApiController]
    [Route("api/listings")]
    public class ListingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly CloudinaryService _cloudinary;

        public ListingsController(AppDbContext context, CloudinaryService cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }

        [HttpGet]
        public async Task<IActionResult> GetListings(
    string? search,
    Guid? categoryId,
    decimal? minPrice,
    decimal? maxPrice,
    int page = 1,
    int pageSize = 10)
        {
            var query = _context.Listings.AsNoTracking()
                .Where(l => l.Status == ListingStatus.Active);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(l => l.Title.Contains(search));

            if (categoryId.HasValue)
                query = query.Where(l => l.CategoryId == categoryId);

            if (minPrice.HasValue)
                query = query.Where(l => l.Price >= minPrice);

            if (maxPrice.HasValue)
                query = query.Where(l => l.Price <= maxPrice);

            var listings = await query
                .OrderByDescending(l => l.IsPromoted)
                .ThenByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new
                {
                    l.Id,
                    l.Title,
                    l.Price,
                    l.Location,
                    l.IsPromoted,
                    Image = l.Images
                        .Where(i => i.IsPrimary)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(listings);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetListing(Guid id)
        {
            var listing = await _context.Listings
                .Where(l => l.Id == id)
                .Select(l => new
                {
                    l.Id,
                    l.Title,
                    l.Description,
                    l.Price,
                    l.Location,
                    l.Condition,
                    l.Brand,

                    Images = l.Images.Select(i => i.ImageUrl),
                    Tags = l.Tags.Select(t => t.Tag),

                    Seller = new
                    {
                        l.Seller.Id,
                        l.Seller.FirstName,
                        l.Seller.LastName
                    },

                    Store = new
                    {
                        l.Store.Id,
                        l.Store.Name
                    }
                })
                .FirstOrDefaultAsync();

            if (listing == null) return NotFound();

            // povećaj views
            var entity = await _context.Listings.FindAsync(id);
            entity.ViewsCount++;
            await _context.SaveChangesAsync();

            return Ok(listing);
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateListing([FromForm] CreateListingDto dto)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = Guid.Parse(userIdStr);

            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (store == null) return BadRequest("Store not found");

            var listing = new Listing
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.Price,
                Currency = dto.Currency,
                CategoryId = dto.CategoryId,
                SubcategoryId = dto.SubcategoryId,
                Location = dto.Location,
                Condition = dto.Condition,
                Brand = dto.Brand,
                SellerId = userId,
                StoreId = store.Id
            };

            _context.Listings.Add(listing);

            // TAGS
            if (dto.Tags != null)
            {
                listing.Tags = dto.Tags.Select(t => new ListingTag
                {
                    Id = Guid.NewGuid(),
                    Tag = t
                }).ToList();
            }

            // IMAGES (max 8)
            if (dto.Images != null && dto.Images.Count > 8)
                return BadRequest("Max 8 images");

            if (dto.Images != null)
            {
                int order = 0;

                foreach (var file in dto.Images)
                {
                    var url = await _cloudinary.UploadImageAsync(file);

                    listing.Images.Add(new ListingImage
                    {
                        Id = Guid.NewGuid(),
                        ImageUrl = url,
                        SortOrder = order,
                        IsPrimary = order == 0
                    });

                    order++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(listing.Id);
        }
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateListing(Guid id, CreateListingDto dto)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = Guid.Parse(userIdStr);

            var listing = await _context.Listings
                .Include(l => l.Tags)
                .FirstOrDefaultAsync(l => l.Id == id && l.SellerId == userId);

            if (listing == null) return NotFound();

            listing.Title = dto.Title;
            listing.Description = dto.Description;
            listing.Price = dto.Price;
            listing.Location = dto.Location;

            // update tags
            listing.Tags.Clear();
            if (dto.Tags != null)
            {
                listing.Tags = dto.Tags.Select(t => new ListingTag
                {
                    Id = Guid.NewGuid(),
                    Tag = t
                }).ToList();
            }

            await _context.SaveChangesAsync();

            return Ok();
        }
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteListing(Guid id)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = Guid.Parse(userIdStr);

            var listing = await _context.Listings
                .Include(l => l.Images)
                .Include(l => l.Tags)
                .Include(l => l.Likes)
                .Include(l => l.Reports)
                .FirstOrDefaultAsync(l => l.Id == id && l.SellerId == userId);

            if (listing == null) return NotFound();

            _context.ListingImages.RemoveRange(listing.Images);
            _context.ListingTags.RemoveRange(listing.Tags);
            _context.Likes.RemoveRange(listing.Likes);
            _context.ListingReports.RemoveRange(listing.Reports);

            _context.Listings.Remove(listing);

            await _context.SaveChangesAsync();

            return Ok();
        }
        [Authorize]
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeStatus(Guid id, ListingStatus status)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = Guid.Parse(userIdStr);

            var listing = await _context.Listings
                .FirstOrDefaultAsync(l => l.Id == id && l.SellerId == userId);

            if (listing == null) return NotFound();

            listing.Status = status;

            await _context.SaveChangesAsync();

            return Ok();
        }
        [Authorize]
        [HttpPost("{id}/promote")]
        public async Task<IActionResult> Promote(Guid id)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = Guid.Parse(userIdStr);

            var listing = await _context.Listings
                .FirstOrDefaultAsync(l => l.Id == id && l.SellerId == userId);

            if (listing == null) return NotFound();

            listing.IsPromoted = true;
            listing.RenewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok();
        }
        [Authorize]
        [HttpPost("{id}/like")]
        public async Task<IActionResult> Like(Guid id)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = Guid.Parse(userIdStr);

            var exists = await _context.Likes
                .AnyAsync(l => l.UserId == userId && l.ListingId == id);

            if (exists) return BadRequest("Already liked");

            _context.Likes.Add(new Like
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ListingId = id
            });

            await _context.SaveChangesAsync();

            return Ok();
        }

        [Authorize]
        [HttpDelete("{id}/like")]
        public async Task<IActionResult> Unlike(Guid id)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = Guid.Parse(userIdStr);

            var like = await _context.Likes
                .FirstOrDefaultAsync(l => l.UserId == userId && l.ListingId == id);

            if (like == null) return NotFound();

            _context.Likes.Remove(like);

            await _context.SaveChangesAsync();

            return Ok();
        }
        [Authorize]
        [HttpPost("{id}/report")]
        public async Task<IActionResult> Report(Guid id, string reason)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = Guid.Parse(userIdStr);

            _context.ListingReports.Add(new ListingReport
            {
                Id = Guid.NewGuid(),
                ListingId = id,
                ReporterId = userId,
                Reason = reason
            });

            var listing = await _context.Listings.FindAsync(id);
            listing.ReportsCount++;

            await _context.SaveChangesAsync();

            return Ok();
        }
        [Authorize]
        [HttpGet("my")]
        public async Task<IActionResult> MyListings()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = Guid.Parse(userIdStr);

            var listings = await _context.Listings
                .Where(l => l.SellerId == userId)
                .Select(l => new
                {
                    l.Id,
                    l.Title,
                    l.Price,
                    l.Status,
                    l.ViewsCount
                })
                .ToListAsync();

            return Ok(listings);
        }



    }
}
