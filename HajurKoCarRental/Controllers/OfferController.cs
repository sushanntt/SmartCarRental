using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HajurKoCarRental.Data;
using HajurKoCarRental.Models;
using Microsoft.AspNetCore.Identity;
using HajurKoCarRental.Areas.Identity.Data;
using System.Net.Mail;
using System.Net;

using HajurKoCarRental.Services;

namespace HajurKoCarRental.Controllers
{
    // OfferController to handle offer rental requests
    public class OfferController : Controller
    {
        private readonly HajurKoCarRentalContext _context;
        private readonly IWebHostEnvironment _webHost;
		private readonly UserManager<HajurKoCarRentalUser> _userManager;
        private readonly NotificationBadgeService _notificationBadgeService;
        private readonly IConfiguration _configuration;

		public OfferController(
            HajurKoCarRentalContext context,
            IWebHostEnvironment webHost,
            UserManager<HajurKoCarRentalUser> userManager,
            NotificationBadgeService notificationBadgeService,
            IConfiguration configuration)
        {
            _context = context;
            _webHost = webHost;
            _userManager = userManager;
            _notificationBadgeService = notificationBadgeService;
            _configuration = configuration;
        }

        // GET: Offer
        public async Task<IActionResult> Index()
        {
			var customers = await _userManager.GetUsersInRoleAsync("Customer");
			return _context.Offer != null ? 
                          View(await _context.Offer.Include(o => o.OfferBranches).ThenInclude(ob => ob.Branch).ToListAsync()) :
                          Problem("Entity set 'HajurKoCarRentalContext.Offer'  is null.");
        }

        // GET: Offer/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null || _context.Offer == null)
            {
                return NotFound();
            }

            var offer = await _context.Offer
                .Include(o => o.OfferBranches)
                    .ThenInclude(ob => ob.Branch)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (offer == null)
            {
                return NotFound();
            }

            return View(offer);
        }

        // If the offer is taken it changes the availability of the offer
        public async Task<IActionResult> UpdateOfferAvailability()
        {
            var offers = await _context.Offer!.ToListAsync();

            foreach (var offer in offers)
            {
                if (offer.ValidityPeroid < DateTime.Now)
                {
                    offer.Available = false;
                }
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }
        // GET: Offer/Create
        public IActionResult Create()
        {
            ViewBag.Branches = _context.Branches!.ToList();
            return View();
        }

        // Creates new offer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Offer offer, string[] branchIds, int[] branchCounts)
        {
            string uniqueFileName = GetProfilePhotoFileName(offer);
            offer.PhotoUrl = uniqueFileName;

            if (branchIds != null)
            {
                for (int i = 0; i < branchIds.Length; i++)
                {
                    if (Guid.TryParse(branchIds[i], out var branchId))
                    {
                        var count = (branchCounts != null && i < branchCounts.Length) ? branchCounts[i] : 1;
                        offer.OfferBranches.Add(new OfferBranch
                        {
                            Id = Guid.NewGuid(),
                            OfferId = offer.Id,
                            BranchId = branchId,
                            AvailableCount = count
                        });
                    }
                }
            }

            _context.Add(offer);
			_context.SaveChanges();
            var customers = await _userManager.GetUsersInRoleAsync("Customer");
            var from = _configuration["Email:FromAddress"];
            var password = _configuration["Email:Password"];

            if (!string.IsNullOrWhiteSpace(from) && !string.IsNullOrWhiteSpace(password))
            {
                foreach (var customer in customers)
                {
                    try
                    {
                        await GmailSender.SendAsync(
                            from,
                            password,
                            customer.Email,
                            "New Offer Available",
                            "Dear Customer,\nWe have added a new offer. Please visit our website to view the details.\nThank you."
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            TempData["SuccessMessage"] = "Offer Added Successful!";
            return RedirectToAction(nameof(Index));
          
        }

        // GET: Offer/Edit/5 Returns user to the offer page by getting selected offer data
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null || _context.Offer == null)
            {
                return NotFound();
            }

            var offer = await _context.Offer
                .Include(o => o.OfferBranches)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (offer == null)
            {
                return NotFound();
            }
            ViewBag.Branches = _context.Branches!.ToList();
            return View(offer);
        }
        
        // Updates the offer details
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Offer offer, string[] branchIds, int[] branchCounts)
        {
            if (offer.Photo != null)
            {
                string uniqueFileName = GetProfilePhotoFileName(offer);
                offer.PhotoUrl = uniqueFileName;
            }
            else
            {
                var existingOffer = _context.Offer.AsNoTracking().FirstOrDefault(o => o.Id == offer.Id);
                offer.PhotoUrl = existingOffer?.PhotoUrl;
            }

            var existingBranches = _context.OfferBranches!.Where(ob => ob.OfferId == offer.Id).ToList();
            _context.OfferBranches!.RemoveRange(existingBranches);

            if (branchIds != null)
            {
                for (int i = 0; i < branchIds.Length; i++)
                {
                    if (Guid.TryParse(branchIds[i], out var branchId))
                    {
                        var count = (branchCounts != null && i < branchCounts.Length) ? branchCounts[i] : 1;
                        _context.OfferBranches!.Add(new OfferBranch
                        {
                            Id = Guid.NewGuid(),
                            OfferId = offer.Id,
                            BranchId = branchId,
                            AvailableCount = count
                        });
                    }
                }
            }

            _context.Attach(offer);
            _context.Entry(offer).State = EntityState.Modified;
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        // GET: Offer/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null || _context.Offer == null)
            {
                return NotFound();
            }

            var offer = await _context.Offer
                .FirstOrDefaultAsync(m => m.Id == id);
            if (offer == null)
            {
                return NotFound();
            }

            return View(offer);
        }

        // POST: Offer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (_context.Offer == null)
            {
                return Problem("Entity set 'HajurKoCarRentalContext.Offer'  is null.");
            }
            var offer = await _context.Offer.FindAsync(id);
            if (offer != null)
            {
                _context.Offer.Remove(offer);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Checks if the offer exists
        private bool OfferExists(Guid id)
        {
          return (_context.Offer?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        //Copies the selected photo to the images folder of the wwwroot folder and returns the url to the image
        private string GetProfilePhotoFileName(Offer offer)
        {
            string uniqueFileName = null;

            if (offer.Photo != null)
            {
                string uploadsFolder = Path.Combine(_webHost.WebRootPath, "images");
                uniqueFileName = Guid.NewGuid().ToString() + "_" + offer.Photo.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    offer.Photo.CopyTo(fileStream);
                }
            }
            return uniqueFileName;
        }

        // Sends mail to notifying about the offer
        [HttpPost]
        public async Task<IActionResult> SendMail()
        {
            var customers = await _userManager.GetUsersInRoleAsync("Customer");
            var from = _configuration["Email:FromAddress"];
            var password = _configuration["Email:Password"];

            if (!string.IsNullOrWhiteSpace(from) && !string.IsNullOrWhiteSpace(password))
            {
                foreach (var customer in customers)
                {
                    try
                    {
                        await GmailSender.SendAsync(
                            from,
                            password,
                            customer.Email,
                            "Offer Offer",
                            "Dear Customer,\nPlease do visit our website to view our new offers.\nThank You"
                        );
                    }
                    catch (Exception ex)
                    {
                        TempData["ErrorMessage"] = $"Error: {ex.Message}";
                        return RedirectToAction("Index");
                    }
                }
            }

            TempData["SuccessMessage"] = "Email sent successfully.";
            return RedirectToAction("Index");
        }

        // Adds offer request by user to the database
        public async Task<IActionResult> OfferRequest()
        {
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            if (User.IsInRole("Customer"))
            {
                await _notificationBadgeService.MarkCustomerRentOffersSeenAsync(currentUser!.Id);
                var hajurKoCarRentalContext = _context.RentOffer!.Include(r => r.Offer).Include(r => r.PickupBranch).Where(r => r.UserId == currentUser.Id);
                return View(await hajurKoCarRentalContext.ToListAsync());
            }

            await _notificationBadgeService.MarkAdminRentOffersSeenAsync();
            var allDataContext = _context.RentOffer!.Include(r => r.Offer).Include(r => r.User).Include(r => r.PickupBranch);
            return View(await allDataContext.ToListAsync());
        }

        // Approval of offer by staff or admin
        public async Task<IActionResult> ApproveOfferCarRequest(Guid? id)
        {
            try
            {
                _context.Database.ExecuteSqlRaw("ALTER TABLE RentOffer MODIFY COLUMN Verified TINYINT(1) NULL;");
            }
            catch (Exception) { }

            RentOffer offer = _context.RentOffer!.Include(r => r.Offer).FirstOrDefault(r => r.Id == id)!;
            offer.Verified = true;
            offer.CustomerSeen = false;
            _context.Update(offer);
            await _context.SaveChangesAsync();

            // Decrement OfferBranch available count
            if (offer.PickupBranchId.HasValue)
            {
                var offerBranch = await _context.OfferBranches!
                    .FirstOrDefaultAsync(ob => ob.OfferId == offer.OfferId && ob.BranchId == offer.PickupBranchId.Value);
                if (offerBranch != null && offerBranch.AvailableCount > 0)
                {
                    offerBranch.AvailableCount--;
                    _context.Update(offerBranch);
                }
            }

            var user = await _userManager.FindByIdAsync(offer.UserId);
            string userEmail = user?.Email ?? offer.UserId;
            await LogAudit("Approved", "RentOffer", id.ToString(), $"User: {userEmail}, Car: {offer.Offer?.CarName ?? offer.OfferId.ToString()}, Amount: {offer.Amount}");
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Rent Request Approved Successfully!";
            string Email = user.Email;
            string to = Email;

            try
            {
                var from = _configuration["Email:FromAddress"];
                var password = _configuration["Email:Password"];

                if (!string.IsNullOrWhiteSpace(from) && !string.IsNullOrWhiteSpace(password))
                {
                    await GmailSender.SendAsync(
                        from,
                        password,
                        to,
                        "Approved Rental Request",
                        $"Dear Customer,\nYour car rental request has been approved.\nThank You"
                    );
                    TempData["SuccessMessage"] = "Offer approved and notification email sent.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Offer approval email failed: {ex.Message}";
            }
            return RedirectToAction("OfferRequest");
        }

        // Gets offer by id
        private Offer GetOfferById(Guid id)
        {
            var offer = _context.Offer.FirstOrDefault(c => c.Id == id);
            return offer;
        }

        // Handle offer cancel by user
        public async Task<IActionResult> RejectOfferCarRequest(Guid? id)
        {
            try
            {
                _context.Database.ExecuteSqlRaw("ALTER TABLE RentOffer MODIFY COLUMN Verified TINYINT(1) NULL;");
            }
            catch (Exception) { }

            RentOffer rentoffer = _context.RentOffer!.FirstOrDefault(r => r.Id == id)!;
            var offerID = rentoffer.OfferId;
            Offer offer = GetOfferById(offerID);
            offer.ApprovedStatus = false;
            offer.IsBooked= false;   
            _context.Update(offer);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Rent Request Removed Successfully";
            if (rentoffer.PaymentMethod == "Khalti")
            {
                int refundAmount = Convert.ToInt32(Math.Round(rentoffer.AdvancePaid > 0 ? rentoffer.AdvancePaid : rentoffer.Amount));
                var refundableRent = new RefundableRent
                {
                    UserId = rentoffer.UserId,
                    Amount = refundAmount,
                    Status = false,
                    AdminSeen = false,
                    CustomerSeen = false
                };
                _context.RefundableRent!.Add(refundableRent);
                await _context.SaveChangesAsync();

                var user = await _userManager.FindByIdAsync(rentoffer.UserId);
                string Email = user.Email;
                string to = Email;

                try
                {
                    var from = _configuration["Email:FromAddress"];
                    var password = _configuration["Email:Password"];

                    if (!string.IsNullOrWhiteSpace(from) && !string.IsNullOrWhiteSpace(password))
                    {
                        await GmailSender.SendAsync(
                            from,
                            password,
                            to,
                            "Refund",
                            $"Dear Customer,\nSince, You cancelled your rental request.Please, visit our company for the amount or you can email your account number for refund amount.\nThank You"
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            var rejectUser = await _userManager.FindByIdAsync(rentoffer.UserId);
            await LogAudit("Rejected", "RentOffer", id.ToString(), $"Admin rejected offer request. User: {rejectUser?.Email ?? rentoffer.UserId}, Car: {offer.CarName}, Amount: {rentoffer.Amount}");

            _context.RentOffer.Remove(rentoffer);
            await _context.SaveChangesAsync();
            return RedirectToAction("OfferRequest");
        }

        public async Task<IActionResult> OfferRequestDetails(Guid? id)
        {
            if (id == null) return NotFound();
            var rentOffer = await _context.RentOffer!
                .Include(r => r.Offer)
                .Include(r => r.User)
                .Include(r => r.PickupBranch)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (rentOffer == null) return NotFound();
            return View(rentOffer);
        }

        // Customer removes their own pending offer request
        public async Task<IActionResult> CustomerRemoveOfferRent(Guid? id)
        {
            if (id == null) return NotFound();
            var rentOffer = await _context.RentOffer!.Include(r => r.Offer).FirstOrDefaultAsync(r => r.Id == id);
            if (rentOffer == null) return NotFound();

            if (rentOffer.Verified == true)
            {
                TempData["ErrorMessage"] = "Cannot remove an approved offer request.";
                return RedirectToAction("OfferRequest");
            }

            var offer = rentOffer.Offer;
            if (offer != null)
            {
                offer.IsBooked = false;
                _context.Update(offer);
            }

            var cancelUser = await _userManager.FindByIdAsync(rentOffer.UserId);
            await LogAudit("Cancelled", "RentOffer", id.ToString(), $"Customer removed pending offer request. User: {cancelUser?.Email ?? rentOffer.UserId}, Car: {rentOffer.Offer?.CarName ?? rentOffer.OfferId.ToString()}, Amount: {rentOffer.Amount}");

            _context.RentOffer.Remove(rentOffer);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Offer request removed successfully.";
            return RedirectToAction("OfferRequest");
        }

        // Admin removes an approved offer request (with notification)
        public async Task<IActionResult> AdminRemoveApprovedOfferRent(Guid? id)
        {
            if (id == null) return NotFound();
            var rentOffer = await _context.RentOffer!.Include(r => r.Offer).Include(r => r.User).FirstOrDefaultAsync(r => r.Id == id);
            if (rentOffer == null) return NotFound();

            var offer = rentOffer.Offer;
            if (offer != null)
            {
                offer.IsBooked = false;
                _context.Update(offer);
            }

            if (rentOffer.PaymentMethod == "Khalti")
            {
                int refundAmount = Convert.ToInt32(Math.Round(rentOffer.AdvancePaid > 0 ? rentOffer.AdvancePaid : rentOffer.Amount));
                var refundableRent = new RefundableRent
                {
                    UserId = rentOffer.UserId,
                    Amount = refundAmount,
                    Status = false,
                    AdminSeen = false,
                    CustomerSeen = false
                };
                _context.RefundableRent!.Add(refundableRent);
            }

            // Restore OfferBranch available count
            if (rentOffer.PickupBranchId.HasValue)
            {
                var offerBranch = await _context.OfferBranches!
                    .FirstOrDefaultAsync(ob => ob.OfferId == rentOffer.OfferId && ob.BranchId == rentOffer.PickupBranchId.Value);
                if (offerBranch != null)
                {
                    offerBranch.AvailableCount++;
                    _context.Update(offerBranch);
                }
            }

            var user = rentOffer.User;
            if (user != null)
            {
                string to = user.Email;
                try
                {
                    var from = _configuration["Email:FromAddress"];
                    var password = _configuration["Email:Password"];
                    if (!string.IsNullOrWhiteSpace(from) && !string.IsNullOrWhiteSpace(password))
                    {
                        await GmailSender.SendAsync(from, password, to, "Offer Request Removed",
                            $"Dear Customer,\nYour approved offer rental request has been removed by the admin.\nThank You");
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Notification email failed: {ex.Message}";
                }
            }

            await LogAudit("Removed", "RentOffer", id.ToString(), $"Admin removed approved offer request. User: {rentOffer.User?.Email ?? rentOffer.UserId}, Car: {rentOffer.Offer?.CarName ?? rentOffer.OfferId.ToString()}, Amount: {rentOffer.Amount}");

            _context.RentOffer.Remove(rentOffer);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Approved offer request removed successfully.";
            return RedirectToAction("OfferRequest");
        }

        // Admin returns an approved offer (car returned by customer)
        public async Task<IActionResult> ReturnOfferRent(Guid? id)
        {
            if (id == null) return NotFound();
            var rentOffer = await _context.RentOffer!.Include(r => r.Offer).Include(r => r.User).FirstOrDefaultAsync(r => r.Id == id);
            if (rentOffer == null) return NotFound();

            // Restore OfferBranch available count
            if (rentOffer.PickupBranchId.HasValue)
            {
                var offerBranch = await _context.OfferBranches!
                    .FirstOrDefaultAsync(ob => ob.OfferId == rentOffer.OfferId && ob.BranchId == rentOffer.PickupBranchId.Value);
                if (offerBranch != null)
                {
                    offerBranch.AvailableCount++;
                    _context.Update(offerBranch);
                }
            }

            var offer = rentOffer.Offer;
            if (offer != null)
            {
                offer.IsBooked = false;
                _context.Update(offer);
            }

            await LogAudit("Returned", "RentOffer", id.ToString(), $"Offer car returned. User: {rentOffer.User?.Email ?? rentOffer.UserId}, Car: {rentOffer.Offer?.CarName ?? rentOffer.OfferId.ToString()}, Amount: {rentOffer.Amount}");

            _context.RentOffer.Remove(rentOffer);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Offer car returned successfully.";
            return RedirectToAction("OfferRequest");
        }

        private async Task LogAudit(string action, string entityType, string entityId, string details)
        {
            var admin = await _userManager.GetUserAsync(HttpContext.User);
            _context.AuditLogs!.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = admin.Id,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                Timestamp = DateTime.Now
            });
        }

    }
}
