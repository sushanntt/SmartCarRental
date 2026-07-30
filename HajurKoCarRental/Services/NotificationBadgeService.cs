using HajurKoCarRental.Data;
using Microsoft.EntityFrameworkCore;

namespace HajurKoCarRental.Services
{
    public class SidebarNotificationCounts
    {
        public int RentalRequest { get; set; }
        public int OfferRentRequest { get; set; }
        public int RefundDetails { get; set; }
        public int DamageRequest { get; set; }
        public int LateFeePending { get; set; }
        public int RentalHistory { get; set; }
        public int ReturnCar { get; set; }
        public int OverdueRental { get; set; }
        public int FrequentCustomer { get; set; }
        public int SalesReport { get; set; }
        public int ChatUnread { get; set; }
    }

    public class NotificationBadgeService
    {
        private readonly HajurKoCarRentalContext _context;

        public NotificationBadgeService(HajurKoCarRentalContext context)
        {
            _context = context;
        }

        public async Task<SidebarNotificationCounts> GetCustomerCountsAsync(string userId)
        {
            return new SidebarNotificationCounts
            {
                RentalRequest = await _context.Rent!
                    .CountAsync(r => r.UserId == userId && !r.CustomerSeen),
                OfferRentRequest = await _context.RentOffer!
                    .CountAsync(r => r.UserId == userId && !r.CustomerSeen),
                RefundDetails = await _context.RefundableRent!
                    .CountAsync(r => r.UserId == userId && !r.CustomerSeen),
                DamageRequest = await _context.CarDamage!
                    .CountAsync(d => d.UserId == userId && !d.CustomerSeen),
                LateFeePending = await _context.LateFees!
                    .CountAsync(f => f.UserId == userId && f.Status == "Unpaid"),
                RentalHistory = await _context.RentalHistory!
                    .CountAsync(r => r.UserId == userId && !r.Seen)
            };
        }

        public async Task<SidebarNotificationCounts> GetAdminCountsAsync(string adminId = null)
        {
            return new SidebarNotificationCounts
            {
                RentalRequest = await _context.Rent!
                    .CountAsync(r => !r.Verified && r.Status == "Pending" && !r.AdminSeen),
                OfferRentRequest = await _context.RentOffer!
                    .CountAsync(r => r.Verified == false && !r.AdminSeen),
                RefundDetails = await _context.RefundableRent!
                    .CountAsync(r => r.Status != true && !r.AdminSeen),
                ReturnCar = await _context.Rent!
                    .CountAsync(r => r.Verified && !r.ReturnSeen),
                OverdueRental = await OverdueRentalCountAsync(),
                FrequentCustomer = 0,
                SalesReport = await _context.RentalHistory!
                    .CountAsync(r => r.Status && !r.AdminSeen),
                DamageRequest = await _context.CarDamage!
                    .CountAsync(d => !d.AdminSeen),
                LateFeePending = await _context.LateFees!
                    .CountAsync(f => f.Status == "Unpaid"),
                ChatUnread = adminId != null
                    ? await _context.ChatMessage!
                        .CountAsync(m => m.ReceiverId == adminId && !m.IsRead)
                    : 0
            };
        }

        private async Task<int> OverdueRentalCountAsync()
        {
            var allApproved = await _context.Rent!
                .Where(r => r.Verified)
                .ToListAsync();
            return allApproved.Count(r => RentalPricingHelper.ExpectedReturn(r.RentalDate, r.Duration, r.Hours) < DateTime.Now);
        }

        public async Task MarkCustomerRentsSeenAsync(string userId)
        {
            var unseen = await _context.Rent!
                .Where(r => r.UserId == userId && !r.CustomerSeen)
                .ToListAsync();
            if (unseen.Count == 0) return;
            foreach (var item in unseen) item.CustomerSeen = true;
            await _context.SaveChangesAsync();
        }

        public async Task MarkAdminRentsSeenAsync()
        {
            var unseen = await _context.Rent!
                .Where(r => !r.Verified && r.Status == "Pending" && !r.AdminSeen)
                .ToListAsync();
            if (unseen.Count == 0) return;
            foreach (var item in unseen) item.AdminSeen = true;
            await _context.SaveChangesAsync();
        }

        public async Task MarkCustomerRentOffersSeenAsync(string userId)
        {
            var unseen = await _context.RentOffer!
                .Where(r => r.UserId == userId && !r.CustomerSeen)
                .ToListAsync();
            if (unseen.Count == 0) return;
            foreach (var item in unseen) item.CustomerSeen = true;
            await _context.SaveChangesAsync();
        }

        public async Task MarkAdminRentOffersSeenAsync()
        {
            var unseen = await _context.RentOffer!
                .Where(r => r.Verified == false && !r.AdminSeen)
                .ToListAsync();
            if (unseen.Count == 0) return;
            foreach (var item in unseen) item.AdminSeen = true;
            await _context.SaveChangesAsync();
        }

        public async Task MarkCustomerRefundsSeenAsync(string userId)
        {
            var unseen = await _context.RefundableRent!
                .Where(r => r.UserId == userId && !r.CustomerSeen)
                .ToListAsync();
            if (unseen.Count == 0) return;
            foreach (var item in unseen) item.CustomerSeen = true;
            await _context.SaveChangesAsync();
        }

        public async Task MarkAdminRefundsSeenAsync()
        {
            var unseen = await _context.RefundableRent!
                .Where(r => r.Status != true && !r.AdminSeen)
                .ToListAsync();
            if (unseen.Count == 0) return;
            foreach (var item in unseen) item.AdminSeen = true;
            await _context.SaveChangesAsync();
        }

        public async Task MarkCustomerDamageSeenAsync(string userId)
        {
            var unseen = await _context.CarDamage!
                .Where(d => d.UserId == userId && !d.CustomerSeen)
                .ToListAsync();
            if (unseen.Count == 0) return;
            foreach (var item in unseen) item.CustomerSeen = true;
            await _context.SaveChangesAsync();
        }

        public async Task MarkAdminDamageSeenAsync()
        {
            var unseen = await _context.CarDamage!
                .Where(d => !d.AdminSeen)
                .ToListAsync();
            if (unseen.Count == 0) return;
            foreach (var item in unseen) item.AdminSeen = true;
            await _context.SaveChangesAsync();
        }

        public async Task MarkReturnCarSeenAsync()
        {
            var unseen = await _context.Rent!
                .Where(r => r.Verified && !r.ReturnSeen)
                .ToListAsync();
            if (unseen.Count == 0) return;
            foreach (var item in unseen) item.ReturnSeen = true;
            await _context.SaveChangesAsync();
        }

        public async Task MarkFrequentCustomersSeenAsync()
        {
            var unseen = await _context.Users
                .Where(u => u.RegularCustomer == true && !u.FrequentCustomerSeen)
                .ToListAsync();
            if (unseen.Count == 0) return;
            foreach (var user in unseen) user.FrequentCustomerSeen = true;
            await _context.SaveChangesAsync();
        }

        public async Task MarkAdminSalesReportSeenAsync()
        {
            var unseen = await _context.RentalHistory!
                .Where(r => r.Status && !r.AdminSeen)
                .ToListAsync();
            if (unseen.Count == 0) return;
            foreach (var item in unseen) item.AdminSeen = true;
            await _context.SaveChangesAsync();
        }
    }
}
