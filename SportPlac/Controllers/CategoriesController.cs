using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportPlac.Data;
using SportPlac.Models;
using SportPlac.Models.DTOs;

namespace SportPlac.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Icon = c.Icon,
                    SortOrder = c.SortOrder,

                    Subcategories = c.Subcategories
                        .Where(s => s.IsActive)
                        .Select(s => new SubcategoryDto
                        {
                            Id = s.Id,
                            Name = s.Name
                        }).ToList()
                })
                .ToListAsync();

            return Ok(categories);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateCategory(CreateCategoryDto dto)
        {
            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Icon = dto.Icon,
                SortOrder = dto.SortOrder
            };

            _context.Categories.Add(category);

            await _context.SaveChangesAsync();

            return Ok(category);
        }
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(Guid id, CreateCategoryDto dto)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null) return NotFound();

            category.Name = dto.Name;
            category.Icon = dto.Icon;
            category.SortOrder = dto.SortOrder;

            await _context.SaveChangesAsync();

            return Ok(category);
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var category = await _context.Categories
                .Include(c => c.Subcategories)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            // opcija 1: blokiraj ako ima listinga
            var hasListings = await _context.Listings
                .AnyAsync(l => l.CategoryId == id);

            if (hasListings)
                return BadRequest("Category has listings");

            _context.Subcategories.RemoveRange(category.Subcategories);
            _context.Categories.Remove(category);

            await _context.SaveChangesAsync();

            return Ok();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/subcategories")]
        public async Task<IActionResult> AddSubcategory(Guid id, CreateSubcategoryDto dto)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null) return NotFound();

            var sub = new Subcategory
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                CategoryId = id
            };

            _context.Subcategories.Add(sub);

            await _context.SaveChangesAsync();

            return Ok(sub);
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}/subcategories/{subId}")]
        public async Task<IActionResult> DeleteSubcategory(Guid id, Guid subId)
        {
            var sub = await _context.Subcategories
                .FirstOrDefaultAsync(s => s.Id == subId && s.CategoryId == id);

            if (sub == null) return NotFound();

            // proveri da li ima listinga
            var hasListings = await _context.Listings
                .AnyAsync(l => l.SubcategoryId == subId);

            if (hasListings)
                return BadRequest("Subcategory has listings");

            _context.Subcategories.Remove(sub);

            await _context.SaveChangesAsync();

            return Ok();
        }


    }
}
