using Microsoft.EntityFrameworkCore;
using SportPlac.Data;
using SportPlac.Models;

namespace SportPlac.Services
{
    public interface ISettingsService
    {
        Task<SiteSettings> GetSettingsAsync();
    }
    public class SettingsService : ISettingsService
    {
        private readonly AppDbContext _context;

        public SettingsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SiteSettings> GetSettingsAsync()
        {
            var settings = await _context.SiteSettings
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (settings == null)
            {
                // Vrati podrazumevana podešavanja ako baza nije inicijalizovana
                return new SiteSettings
                {
                    MetaPixelId = "",
                    GooglePixelId = "",
                    AutoWebPConversion = true,
                    SeoImageRenamer = true,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            return settings;
        }
    }

}
