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
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ISettingsService _settingsService;

        public AdminController(AppDbContext context, ISettingsService settingsService)
        {
            _context = context;
            _settingsService = settingsService;
        }
        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalListings = await _context.Listings.CountAsync();

            // fake revenue (ako nemaš payments)
            var revenue = await _context.Subscriptions
                .SumAsync(s => (decimal?)s.Amount) ?? 0;

            return Ok(new
            {
                totalUsers,
                totalListings,
                revenue
            });
        }
        [HttpGet("users")]
        public async Task<IActionResult> Users(string? search, int page = 1, int pageSize = 10)
        {
            var query = _context.Users.AsNoTracking();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u =>
                    u.Email.Contains(search) ||
                    u.FirstName.Contains(search));
            }

            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.Status
                })
                .ToListAsync();

            return Ok(data);
        }
        [HttpPatch("users/{id}/block")]
        public async Task<IActionResult> Block(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.Status = UserStatus.Banned;

            await _context.SaveChangesAsync();
            return Ok();
        }
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok();
        }
        [HttpGet("listings")]
        public async Task<IActionResult> Listings(int page = 1, int pageSize = 10)
        {
            var data = await _context.Listings
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new
                {
                    l.Id,
                    l.Title,
                    l.Status,
                    l.Price
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("listings/reported")]
        public async Task<IActionResult> Reported()
        {
            var data = await _context.Listings
                .Where(l => l.ReportsCount > 0)
                .Select(l => new
                {
                    l.Id,
                    l.Title,
                    l.ReportsCount
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpPatch("listings/{id}/status")]
        public async Task<IActionResult> Status(Guid id, ListingStatus status)
        {
            var listing = await _context.Listings.FindAsync(id);
            if (listing == null) return NotFound();

            listing.Status = status;

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("listings/{id}")]
        public async Task<IActionResult> DeleteListing(Guid id)
        {
            var listing = await _context.Listings.FindAsync(id);
            if (listing == null) return NotFound();

            _context.Listings.Remove(listing);
            await _context.SaveChangesAsync();

            return Ok();
        }
        [HttpGet("subscriptions")]
        public async Task<IActionResult> Subs()
        {
            var data = await _context.Subscriptions
                .Select(s => new
                {
                    s.Id,
                    s.Plan,
                    s.Amount,
                    s.StartDate
                })
                .ToListAsync();

            return Ok(data);
        }
        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var settings = await _settingsService.GetSettingsAsync();
            return Ok(settings);
        }

        [HttpPut("settings")]
        public async Task<IActionResult> UpdateSettings(UpdateSettingsDto dto)
        {
            var settings = await _context.SiteSettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new SiteSettings { Id = Guid.NewGuid() };
                _context.SiteSettings.Add(settings);
            }

            settings.MetaPixelId = dto.MetaPixelId;
            settings.GooglePixelId = dto.GooglePixelId;
            settings.AutoWebPConversion = dto.AutoWebPConversion;
            settings.SeoImageRenamer = dto.SeoImageRenamer;

            // ❌ IGNORIŠEMO BANCA INTESA (po zahtevu)
            settings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(settings);
        }

    }
}
