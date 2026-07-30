using Microsoft.EntityFrameworkCore;
using HajurKoCarRental.Data;
using HajurKoCarRental.Models;

namespace HajurKoCarRental.Services
{
    public class FestivalService
    {
        private readonly HajurKoCarRentalContext _context;
        
        public FestivalService(HajurKoCarRentalContext context)
        {
            _context = context;
        }
        
        public async Task<List<Festival>> GetAllFestivalsAsync()
        {
            return await _context.Festivals
                .Where(f => f.IsActive)
                .OrderBy(f => f.StartDate)
                .ToListAsync();
        }
        
        public async Task<Festival> GetFestivalByIdAsync(int id)
        {
            return await _context.Festivals.FindAsync(id);
        }
        
        public async Task<decimal> GetFestivalMultiplierAsync(DateTime date)
        {
            var festival = await _context.Festivals
                .FirstOrDefaultAsync(f => 
                    f.IsActive && 
                    date >= f.StartDate && 
                    date <= f.EndDate);
            
            return festival?.PriceMultiplier ?? 1.0m;
        }
        
        // Returns the active festival for a given date (or null if none)
        public async Task<Festival> GetActiveFestivalAsync(DateTime date)
        {
            return await _context.Festivals
                .FirstOrDefaultAsync(f =>
                    f.IsActive &&
                    date >= f.StartDate &&
                    date <= f.EndDate);
        }
        
        public async Task<Festival> CreateFestivalAsync(Festival festival)
        {
            _context.Festivals.Add(festival);
            await _context.SaveChangesAsync();
            return festival;
        }
        
        public async Task<Festival> UpdateFestivalAsync(Festival festival)
        {
            _context.Festivals.Update(festival);
            await _context.SaveChangesAsync();
            return festival;
        }
        
        public async Task<bool> DeleteFestivalAsync(int id)
        {
            var festival = await _context.Festivals.FindAsync(id);
            if (festival == null)   // FIXED: was incorrectly "!= null" before
                return false;
                
            _context.Festivals.Remove(festival);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
