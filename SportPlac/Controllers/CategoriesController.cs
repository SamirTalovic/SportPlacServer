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
        private List<SubcategoryTreeDto> BuildTree(
    List<Subcategory> parents,
    List<Subcategory> all)
        {
            return parents.Select(p => new SubcategoryTreeDto
            {
                Id = p.Id,
                Name = p.Name,
                Children = BuildTree(
                    all.Where(x => x.ParentId == p.Id).ToList(),
                    all
                )
            }).ToList();
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .Include(c => c.Subcategories)
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

            var result = categories.Select(c => new
            {
                c.Id,
                c.Name,
                c.Icon,
                c.SortOrder,

                Subcategories = BuildTree(
                    c.Subcategories.Where(s => s.ParentId == null).ToList(),
                    c.Subcategories.ToList()
                )
            });

            return Ok(result);
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

            // Proveri da li kategorija ili BILO KOJA njena podkategorija ima oglase
            var subIds = category.Subcategories.Select(s => s.Id).ToList();
            var hasListings = await _context.Listings
                .AnyAsync(l => l.CategoryId == id || (l.SubcategoryId != null && subIds.Contains(l.SubcategoryId.Value)));

            if (hasListings)
                return BadRequest("Kategorija ili njene podkategorije sadrže oglase i ne mogu biti obrisane.");

            // Obrisi sve podkategorije (uključujući i decu zbog konfigulacije ili ručno)
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
                CategoryId = id,
                ParentId = dto.ParentId
            };

            _context.Subcategories.Add(sub);
            await _context.SaveChangesAsync();

            return Ok(sub);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}/subcategories/{subId}")]
        public async Task<IActionResult> DeleteSubcategory(Guid id, Guid subId)
        {
            // Pronađi podkategoriju i svu njenu decu (rekurzivno)
            var allSubs = await _context.Subcategories.Where(s => s.CategoryId == id).ToListAsync();
            var subToDelete = allSubs.FirstOrDefault(s => s.Id == subId);

            if (subToDelete == null) return NotFound();

            // Funkcija za skupljanje svih ID-jeva u stablu
            var idsToDelete = new List<Guid> { subId };
            void CollectChildren(Guid parentId)
            {
                var children = allSubs.Where(s => s.ParentId == parentId).Select(s => s.Id).ToList();
                foreach (var childId in children)
                {
                    idsToDelete.Add(childId);
                    CollectChildren(childId);
                }
            }
            CollectChildren(subId);

            // Proveri oglase za sve ove ID-jeve
            var hasListings = await _context.Listings
                .AnyAsync(l => l.SubcategoryId != null && idsToDelete.Contains(l.SubcategoryId.Value));

            if (hasListings)
                return BadRequest("Podkategorija ili njeni pod-elementi sadrže oglase.");

            // Brisanje od dece ka roditelju da bi se izbegao FK conflict
            var itemsToRemove = allSubs.Where(s => idsToDelete.Contains(s.Id))
                                       .OrderByDescending(s => s.ParentId != null) // Ovo je gruba aproksimacija
                                       .ToList();

            _context.Subcategories.RemoveRange(itemsToRemove);
            await _context.SaveChangesAsync();

            return Ok();
        }


    }
}
