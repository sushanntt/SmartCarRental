using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HajurKoCarRental.Data;
using HajurKoCarRental.Models;
using System.Runtime.ConstrainedExecution;
using Microsoft.AspNetCore.Identity;
using HajurKoCarRental.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using static HajurKoCarRental.Controllers.PaymentController;
using Microsoft.CodeAnalysis;
using System.Net.Mail;
using System.Net;
using HajurKoCarRental.Services;

namespace HajurKoCarRental.Controllers
{
    // RentsController class to handle extra activities of the rental requests
    [Authorize]
    public class RentsController : Controller
    {
        private readonly HajurKoCarRentalContext _context;
        private readonly UserManager<HajurKoCarRentalUser> _userManager;
        private readonly SignInManager<HajurKoCarRentalUser> _signInManager;
        private readonly NotificationBadgeService _notificationBadgeService;
        private readonly IConfiguration _configuration;

        public RentsController(
            HajurKoCarRentalContext context,
            UserManager<HajurKoCarRentalUser> userManager,
            SignInManager<HajurKoCarRentalUser> signInManager,
            NotificationBadgeService notificationBadgeService,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _notificationBadgeService = notificationBadgeService;
            _configuration = configuration;
        }

        // GET: Rents
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            if (User.IsInRole("Customer"))
            {
                await _notificationBadgeService.MarkCustomerRentsSeenAsync(currentUser!.Id);
                var hajurKoCarRentalContext = _context.Rent!
                    .Include(r => r.Car)
                    .Include(r => r.User)
                    .Include(r => r.PickupBranch)
                    .Where(r => r.UserId == currentUser.Id && r.Status != "Removed" && r.Status != "Rejected");
                return View(await hajurKoCarRentalContext.ToListAsync());
            }

            await _notificationBadgeService.MarkAdminRentsSeenAsync();
            var allDataContext = _context.Rent!
                .Include(r => r.Car)
                .Include(r => r.User)
                .Include(r => r.PickupBranch)
                .Where(r => r.Status != "Removed" && r.Status != "Rejected");
            return View(await allDataContext.ToListAsync());
        }

        // GET: Rents/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null || _context.Rent == null)
            {
                return NotFound();
            }

            var rent = await _context.Rent
                .Include(r => r.Car)
                .Include(r => r.User)
                .Include(r => r.PickupBranch)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (rent == null)
            {
                return NotFound();
            }

            return View(rent);
        }

        // GET: Rents/Create
        public IActionResult Create()
        {
            ViewData["CarId"] = new SelectList(_context.Car, "Id", "Id");
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: Rents/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CarId,UserId,PaymentMethod,Amount,Verified,RequestDate,RentalDate,Duration")] Rent rent)
        {
            if (ModelState.IsValid)
            {
                rent.Id = Guid.NewGuid();
                _context.Add(rent);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CarId"] = new SelectList(_context.Car, "Id", "Id", rent.CarId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", rent.UserId);
            return View(rent);
        }

        // GET: Rents/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null || _context.Rent == null)
            {
                return NotFound();
            }

            var rent = await _context.Rent.FindAsync(id);
            if (rent == null)
            {
                return NotFound();
            }
            var user = await _context.Users.FindAsync(rent.UserId);
            var car = await _context.Car!.FindAsync(rent.CarId);

            ViewData["CarId"] = new SelectList(_context.Car, "Id", "Id", rent.CarId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", rent.UserId);
            /*ViewData["User"] = user;
            ViewData["Car"] = car;*/
            return View(rent);
        }

        // POST: Rents/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,CarId,UserId,PaymentMethod,Amount,Verified,RequestDate,RentalDate,Duration,Hours")] Rent rent)
        {
            var originalRent = await _context.Rent.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            // Preserve fields not in Bind list or missing from form
            rent.PickupBranchId = originalRent?.PickupBranchId;
            rent.PickupLat = originalRent?.PickupLat;
            rent.PickupLng = originalRent?.PickupLng;
            rent.DiscountCode = originalRent?.DiscountCode;
            rent.Duration = originalRent?.Duration ?? rent.Duration;
            rent.Hours = originalRent?.Hours ?? rent.Hours;
            rent.AdvancePaid = originalRent?.AdvancePaid ?? rent.AdvancePaid;
            rent.RemainingAmount = originalRent?.RemainingAmount ?? rent.RemainingAmount;
            bool isTransitionToApproved = rent.Verified && (originalRent == null || !originalRent.Verified);

            if (isTransitionToApproved)
            {
                var car = await _context.Car!.FirstOrDefaultAsync(c => c.Id == rent.CarId);
                if (car != null)
                {
                    if (rent.PickupBranchId.HasValue)
                    {
                        var carBranch = await _context.CarBranches!
                            .FirstOrDefaultAsync(cb => cb.CarId == rent.CarId && cb.BranchId == rent.PickupBranchId.Value);
                        if (carBranch == null || carBranch.AvailableCount <= 0)
                        {
                            TempData["ErrorMessage"] = "Cannot approve booking. No cars available at the selected branch!";
                            return RedirectToAction(nameof(Index));
                        }
                        carBranch.AvailableCount--;
                        _context.Update(carBranch);

                        car.AvailableCarCount = await _context.CarBranches!
                            .Where(cb => cb.CarId == rent.CarId)
                            .SumAsync(cb => cb.AvailableCount);
                    }
                    else
                    {
                        if (car.AvailableCarCount <= 0)
                        {
                            TempData["ErrorMessage"] = "Cannot approve booking. No cars available for this model!";
                            return RedirectToAction(nameof(Index));
                        }
                        car.AvailableCarCount--;
                    }
                    car.IsCarBooked = (car.AvailableCarCount <= 0);
                    _context.Update(car);
                }
            }

            rent.Status = rent.Verified ? "Approved" : "Pending";

            if (isTransitionToApproved)
            {
                rent.CustomerSeen = false;
                rent.ReturnSeen = false;
            }
            else if (originalRent != null && rent.Verified != originalRent.Verified)
            {
                rent.CustomerSeen = false;
            }

            _context.Update(rent);
            await _context.SaveChangesAsync();

            if (isTransitionToApproved)
            {
                var car = await _context.Car!.FirstOrDefaultAsync(c => c.Id == rent.CarId);
                var user = await _userManager.FindByIdAsync(rent.UserId);
                string userEmail = user?.Email ?? rent.UserId;
                await LogAudit("Approved", "Rent", id.ToString(), $"Car: {car?.ModelName ?? "N/A"}, User: {userEmail}, Amount: {rent.Amount}");
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Rent Request Approved Successfully!";

                var currentUser = await _userManager.GetUserAsync(HttpContext.User);
                string Name = currentUser.Firstname + " " + currentUser.Lastname;
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
                        TempData["SuccessMessage"] = "Rent approved and notification email sent to customer.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Email not sent: Email configuration (FromAddress/Password) is missing from appsettings.";
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Rent approved but email failed: {ex.Message}";
                }
            }
            else
            {
                TempData["SuccessMessage"] = "Rent request updated successfully.";
            }
            Rent rentt = _context.Rent!.FirstOrDefault(r => r.Id == id)!;

            ViewData["CarId"] = new SelectList(_context.Car, "Id", "Id", rent.CarId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", rent.UserId);
            return RedirectToAction(nameof(Index));
        }

        // GET: Rents/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null || _context.Rent == null)
            {
                return NotFound();
            }

            var rent = await _context.Rent
                .Include(r => r.Car)
                .Include(r => r.User)
                .Include(r => r.PickupBranch)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (rent == null)
            {
                return NotFound();
            }

            return View(rent);
        }

        // POST: Rents/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (_context.Rent == null)
            {
                return Problem("Entity set 'HajurKoCarRentalContext.Rent' is null.");
            }

            var rent = await _context.Rent
                .Include(r => r.Car)
                .FirstOrDefaultAsync(m => m.Id == id);
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            string Name = currentUser.Firstname + " " + currentUser.Lastname;
            if (rent == null)
            {
                return NotFound();
            }
            bool isCustomerRemoval = currentUser.Id == rent.UserId;

            if (isCustomerRemoval)
            {
                // Customer permanently deletes their own request
                if (rent.Verified)
                {
                    TempData["ErrorMessage"] = "Cannot remove an approved rental request. Contact admin for assistance.";
                    return RedirectToAction(nameof(Index));
                }
                if (rent.PaymentMethod == "Khalti")
                {
                    int refundAmount = Convert.ToInt32(Math.Round(rent.AdvancePaid > 0 ? rent.AdvancePaid : rent.Amount));
                    var refundableRent = new RefundableRent
                    {
                        UserId = rent.UserId,
                        Amount = refundAmount,
                        Status = false,
                        AdminSeen = false,
                        CustomerSeen = true
                    };
                    _context.RefundableRent!.Add(refundableRent);
                    await _context.SaveChangesAsync();

                    var user = await _userManager.FindByIdAsync(rent.UserId);
                    string to = user.Email;
                    try
                    {
                        var from = _configuration["Email:FromAddress"];
                        var password = _configuration["Email:Password"];
                        if (!string.IsNullOrWhiteSpace(from) && !string.IsNullOrWhiteSpace(password))
                        {
                            await GmailSender.SendAsync(from, password, to, "Refund",
                                $"Dear Customer,\nSince you cancelled your rental request, please visit our company for the refund amount or email your account number.\nThank You");
                            TempData["SuccessMessage"] = "Refund notification email sent.";
                        }
                    }
                    catch (Exception ex)
                    {
                        TempData["ErrorMessage"] = $"Refund email failed: {ex.Message}";
                    }
                }

                // Restore availability if it was an approved rent
                if (rent.Verified)
                {
                    var rentCar = await _context.Car!.FirstOrDefaultAsync(c => c.Id == rent.CarId);
                    if (rentCar != null)
                    {
                        if (rent.PickupBranchId.HasValue)
                        {
                            var carBranch = await _context.CarBranches!
                                .FirstOrDefaultAsync(cb => cb.CarId == rent.CarId && cb.BranchId == rent.PickupBranchId.Value);
                            if (carBranch != null)
                            {
                                carBranch.AvailableCount++;
                                _context.Update(carBranch);
                                rentCar.AvailableCarCount = await _context.CarBranches!
                                    .Where(cb => cb.CarId == rent.CarId).SumAsync(cb => cb.AvailableCount);
                            }
                        }
                        else
                        {
                            rentCar.AvailableCarCount++;
                        }
                        rentCar.IsCarBooked = (rentCar.AvailableCarCount <= 0);
                        _context.Update(rentCar);
                        await _context.SaveChangesAsync();
                    }
                }

                _context.Rent.Remove(rent);
                await _context.SaveChangesAsync();

                var cancelUser = await _userManager.FindByIdAsync(rent.UserId);
                TempData["SuccessMessage"] = "Rent Request Deleted Successfully";
                await LogAudit("Cancelled", "Rent", id.ToString(), $"Customer removed pending request. User: {cancelUser?.Email ?? rent.UserId}, Car: {rent.Car?.ModelName ?? rent.CarId.ToString()}, Amount: {rent.Amount}");
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Admin removal
            bool wasApproved = rent.Verified;

            if (wasApproved)
            {
                // Admin removes an approved rent — delete, restore, notify, no history
                if (rent.PickupBranchId.HasValue)
                {
                    var carBranch = await _context.CarBranches!
                        .FirstOrDefaultAsync(cb => cb.CarId == rent.CarId && cb.BranchId == rent.PickupBranchId.Value);
                    if (carBranch != null)
                    {
                        carBranch.AvailableCount++;
                        _context.Update(carBranch);
                        var bookedCar = await _context.Car!.FirstOrDefaultAsync(c => c.Id == rent.CarId);
                        if (bookedCar != null)
                        {
                            bookedCar.AvailableCarCount = await _context.CarBranches!
                                .Where(cb => cb.CarId == rent.CarId).SumAsync(cb => cb.AvailableCount);
                            bookedCar.IsCarBooked = (bookedCar.AvailableCarCount <= 0);
                            _context.Update(bookedCar);
                        }
                    }
                }
                else
                {
                    var bookedCar = await _context.Car!.FirstOrDefaultAsync(c => c.Id == rent.CarId);
                    if (bookedCar != null)
                    {
                        bookedCar.AvailableCarCount++;
                        bookedCar.IsCarBooked = false;
                        _context.Update(bookedCar);
                    }
                }

                string originalPaymentMethod = rent.PaymentMethod ?? "Cash";
                if (originalPaymentMethod == "Khalti")
                {
                    int refundAmount = Convert.ToInt32(Math.Round(rent.AdvancePaid > 0 ? rent.AdvancePaid : rent.Amount));
                    var refundableRent = new RefundableRent
                    {
                        UserId = rent.UserId,
                        Amount = refundAmount,
                        Status = false,
                        AdminSeen = false,
                        CustomerSeen = false
                    };
                    _context.RefundableRent!.Add(refundableRent);
                    await _context.SaveChangesAsync();

                    var refundUser = await _userManager.FindByIdAsync(rent.UserId);
                    string refundTo = refundUser.Email;
                    try
                    {
                        var from = _configuration["Email:FromAddress"];
                        var password = _configuration["Email:Password"];
                        if (!string.IsNullOrWhiteSpace(from) && !string.IsNullOrWhiteSpace(password))
                        {
                            await GmailSender.SendAsync(from, password, refundTo, "Refund",
                                $"Dear Customer,\nYour approved rental request has been removed by admin. Please visit our company for the refund amount or email your account number.\nThank You");
                        }
                    }
                    catch (Exception ex)
                    {
                        TempData["ErrorMessage"] = $"Refund email failed: {ex.Message}";
                    }
                }

                var removedUser = await _userManager.FindByIdAsync(rent.UserId);
                string removedTo = removedUser.Email;
                try
                {
                    var from = _configuration["Email:FromAddress"];
                    var password = _configuration["Email:Password"];
                    if (!string.IsNullOrWhiteSpace(from) && !string.IsNullOrWhiteSpace(password))
                    {
                        await GmailSender.SendAsync(from, password, removedTo, "Rental Request Removed",
                            $"Dear Customer,\nYour approved rental request has been removed by the admin. The car is now available for booking again.\nThank You");
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Notification email failed: {ex.Message}";
                }

                var removeHistory = new RentalHistory
                {
                    UserId = rent.UserId,
                    carId = rent.CarId,
                    AuthorizedBy = currentUser.Firstname + " " + currentUser.Lastname,
                    RentedDate = rent.RentalDate,
                    Status = false,
                    ActionType = "Removed",
                    Duration = rent.Duration,
                    Hours = rent.Hours,
                    Seen = false,
                    AdminSeen = false,
                    Amount = rent.Amount,
                    AdvancePaid = rent.AdvancePaid,
                    RemainingAmount = rent.RemainingAmount,
                    PaymentMethod = rent.PaymentMethod,
                    PickupBranchId = rent.PickupBranchId
                };
                _context.RentalHistory!.Add(removeHistory);

                await LogAudit("Removed", "Rent", id.ToString(), $"Admin removed approved request. Car: {rent.Car?.ModelName ?? rent.CarId.ToString()}, User: {removedUser.Email}, Amount: {rent.Amount}");

                _context.Rent.Remove(rent);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Approved rent request removed successfully.";
                return RedirectToAction(nameof(Index));
            }

            // Admin rejects a pending rent — keep RentalHistory with Status=false
            rent.Status = "Rejected";
            var rentalhistory = new RentalHistory
            {
                UserId = rent.UserId,
                carId = rent.CarId,
                AuthorizedBy = Name,
                RentedDate = rent.RentalDate,
                Status = false,
                ActionType = "Rejected",
                Duration = rent.Duration,
                Hours = rent.Hours,
                Seen = false,
                AdminSeen = false,
                Amount = rent.Amount,
                AdvancePaid = rent.AdvancePaid,
                RemainingAmount = rent.RemainingAmount,
                PaymentMethod = rent.PaymentMethod,
                PickupBranchId = rent.PickupBranchId
            };
            _context.RentalHistory!.Add(rentalhistory);
            rent.CustomerSeen = false;

            string paymentMethod = rent.PaymentMethod ?? "Cash";

            if (paymentMethod == "Khalti")
            {
                int refundAmount = Convert.ToInt32(Math.Round(rent.AdvancePaid > 0 ? rent.AdvancePaid : rent.Amount));
                var refundableRent = new RefundableRent
                {
                    UserId = rent.UserId,
                    Amount = refundAmount,
                    Status = false,
                    AdminSeen = false,
                    CustomerSeen = false
                };
                _context.RefundableRent!.Add(refundableRent);
                await _context.SaveChangesAsync();

                var user = await _userManager.FindByIdAsync(rent.UserId);
                string to = user.Email;
                try
                {
                    var from = _configuration["Email:FromAddress"];
                    var password = _configuration["Email:Password"];
                    if (!string.IsNullOrWhiteSpace(from) && !string.IsNullOrWhiteSpace(password))
                    {
                        await GmailSender.SendAsync(from, password, to, "Refund",
                            $"Dear Customer,\nYour rental request has been rejected. Please visit our company for the refund amount or email your account number.\nThank You");
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Refund email failed: {ex.Message}";
                }
            }

            rent.Verified = false;
            _context.Update(rent);
            await _context.SaveChangesAsync();

            var rejectUser = await _userManager.FindByIdAsync(rent.UserId);
            await LogAudit("Rejected", "Rent", id.ToString(), $"Admin rejected pending request. Car: {rent.Car?.ModelName ?? rent.CarId.ToString()}, User: {rejectUser?.Email ?? rent.UserId}");
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Rent Request Rejected Successfully";
            return RedirectToAction(nameof(Index));
        }

        // This class combines refundable rent model and related user model
        public class refundModel
        {
            
            public List<RefundableRent> RefundableRent { get; set; }
            public List<HajurKoCarRentalUser> Users { get; set; }
        }

        // Returns refund details to the detail view
        public async Task<IActionResult> RefundDetails()
        {
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            var users = await _context.Users.ToListAsync();
            var refund = _context.RefundableRent!.ToList();

            if (User.IsInRole("Customer"))
            {
                await _notificationBadgeService.MarkCustomerRefundsSeenAsync(currentUser!.Id);
                var user = _context.RefundableRent!.Include(r => r.User).Where(r => r.UserId == currentUser.Id).ToList();

                var viewModel = new refundModel
                {
                    RefundableRent = user
                };
                return View(viewModel);
            }
            else
            {
                await _notificationBadgeService.MarkAdminRefundsSeenAsync();
                var viewModel = new refundModel
                {
                    Users = users,
                    RefundableRent = refund
                };
                return View(viewModel);
            }
        }

        // Processes refund done by the staff
        public async Task<IActionResult> Refund(Guid? id)
        {
            RefundableRent refund = _context.RefundableRent!.FirstOrDefault(r => r.Id == id)!;
            refund.Status = true;
            refund.CustomerSeen = false;
            _context.Update(refund);
            await _context.SaveChangesAsync();
            
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", refund.UserId);
            TempData["SuccessMessage"] = "Amount Refunded Successfully";
            return RedirectToAction(nameof(RefundDetails));
        }

        // Customer confirms they received the refund
        public async Task<IActionResult> ConfirmRefund(Guid? id)
        {
            RefundableRent refund = _context.RefundableRent!.FirstOrDefault(r => r.Id == id)!;
            refund.CustomerConfirmed = true;
            refund.CustomerSeen = false;
            _context.Update(refund);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Refund confirmed. Thank you!";
            return RedirectToAction(nameof(RefundDetails));
        }

        // Returnes all the rents
        public async Task<IActionResult> ReturnedCar()
        {
            await _notificationBadgeService.MarkReturnCarSeenAsync();
            var allDataContext = _context.Rent!.Include(r => r.Car).Include(r => r.User);
            return View(await allDataContext.ToListAsync());
        }

        // Marks remaining balance as paid by admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRemainingPaid(Guid id)
        {
            var rent = await _context.Rent!.Include(r => r.Car).Include(r => r.User).FirstOrDefaultAsync(r => r.Id == id);
            if (rent == null)
            {
                TempData["ErrorMessage"] = "Rental not found.";
                return RedirectToAction(nameof(ReturnedCar));
            }

            if (rent.RemainingAmount <= 0)
            {
                TempData["SuccessMessage"] = "No remaining balance to mark.";
                return RedirectToAction(nameof(ReturnedCar));
            }

            rent.RemainingAmount = 0;
            _context.Update(rent);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Remaining balance marked as paid.";
            return RedirectToAction(nameof(ReturnedCar));
        }

        // Shows only overdue rentals (approved and past expected return date)
        public async Task<IActionResult> OverdueRentals()
        {
            var allRents = await _context.Rent!
                .Include(r => r.Car)
                .Include(r => r.User)
                .Where(r => r.Verified)
                .ToListAsync();
            var overdue = allRents.Where(r => RentalPricingHelper.ExpectedReturn(r.RentalDate, r.Duration, r.Hours) < DateTime.Now).ToList();
            return View(overdue);
        }

        [HttpPost]
        public async Task<IActionResult> SendOverdueEmail(string email, string subject, string message)
        {
            try
            {
                var from = _configuration["Email:FromAddress"];
                var password = _configuration["Email:Password"];
                await GmailSender.SendAsync(from, password, email, subject, message);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // Gets car model by id
        private Car GetCarById(Guid id)
        {
            var car = _context.Car!.Include(c => c.CarBrand).FirstOrDefault(c => c.Id == id);
            return car;
        }

        // Executes when returning the car
        public async Task<IActionResult> Return(Guid id, Rent rent)
        {
            rent = await _context.Rent.Include(r => r.Car).FirstOrDefaultAsync(m => m.Id == id);

            if (rent != null)
            {
                if (rent.RemainingAmount > 0)
                {
                    TempData["ErrorMessage"] = "Cannot return car. Remaining balance of Rs. " + rent.RemainingAmount + " must be marked as paid first.";
                    return RedirectToAction(nameof(ReturnedCar));
                }

                var carId = rent.CarId;
                Car car = GetCarById(carId);

                if (rent.PickupBranchId.HasValue)
                {
                    var carBranch = await _context.CarBranches!
                        .FirstOrDefaultAsync(cb => cb.CarId == carId && cb.BranchId == rent.PickupBranchId.Value);
                    if (carBranch != null)
                    {
                        carBranch.AvailableCount++;
                        _context.Update(carBranch);

                        car.AvailableCarCount = await _context.CarBranches!
                            .Where(cb => cb.CarId == carId)
                            .SumAsync(cb => cb.AvailableCount);
                    }
                }
                else
                {
                    car.AvailableCarCount++;
                }
                car.IsCarBooked = (car.AvailableCarCount <= 0);
                _context.Update(car);

                var currentUser = await _userManager.GetUserAsync(HttpContext.User);
                string authorizedBy = currentUser.Firstname + " " + currentUser.Lastname;

                // Calculate late fee
                var expectedReturn = RentalPricingHelper.ExpectedReturn(rent.RentalDate, rent.Duration, rent.Hours);
                var lateHours = (DateTime.Now - expectedReturn).TotalHours;
                double dailyRate = car.Price;
                double lateFeeAmount = 0;

                if (lateHours > 2)
                {
                    if (lateHours <= 6)
                    {
                        lateFeeAmount = dailyRate * 0.30;
                    }
                    else if (lateHours <= 24)
                    {
                        lateFeeAmount = dailyRate;
                    }
                    else
                    {
                        int fullDaysLate = (int)Math.Ceiling(lateHours / 24);
                        lateFeeAmount = dailyRate * fullDaysLate;
                    }
                }

                if (lateFeeAmount > 0)
                {
                    var lateFee = new LateFee
                    {
                        Id = Guid.NewGuid(),
                        RentId = rent.Id,
                        CarId = rent.CarId,
                        UserId = rent.UserId,
                        Amount = Math.Round(lateFeeAmount, 2),
                        HoursLate = Math.Round(lateHours, 2),
                        Status = "Unpaid",
                        CustomerConfirmed = false,
                        CreatedAt = DateTime.Now
                    };
                    _context.LateFees!.Add(lateFee);

                    var user = await _userManager.FindByIdAsync(rent.UserId);
                    if (user != null)
                    {
                        string to = user.Email;
                        try
                        {
                            var from = _configuration["Email:FromAddress"];
                            var password = _configuration["Email:Password"];
                            if (!string.IsNullOrWhiteSpace(from) && !string.IsNullOrWhiteSpace(password))
                            {
                                string tierMsg = lateHours <= 6 ? "30% of daily rate (Rs. " + Math.Round(dailyRate * 0.30, 2) + ")"
                                    : lateHours <= 24 ? "1 day extra charge (Rs. " + Math.Round(dailyRate, 2) + ")"
                                    : Math.Ceiling(lateHours / 24) + " day(s) extra charge (Rs. " + Math.Round(lateFeeAmount, 2) + ")";
                                await GmailSender.SendAsync(from, password, to, "Late Return Fee Applied",
                                    $"Dear Customer,\nYour rented car ({car.ModelName}) was returned {Math.Round(lateHours, 2)} hour(s) late.\nLate fee: Rs. {Math.Round(lateFeeAmount, 2)}\nTier: {tierMsg}\nPlease clear the fee to rent again.\nThank You");
                            }
                        }
                        catch (Exception ex)
                        {
                            TempData["ErrorMessage"] = $"Late fee calculated but email failed: {ex.Message}";
                        }
                    }

                    TempData["SuccessMessage"] = "Returned Car Added. Late fee of Rs. " + Math.Round(lateFeeAmount, 2) + " applied. Manage in Late Fees.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Returned Car Added. Returned on time.";
                }

                var rentalhistory = new RentalHistory
                {
                    UserId = rent.UserId,
                    carId = rent.CarId,
                    AuthorizedBy = authorizedBy,
                    RentedDate = rent.RentalDate,
                    Status = true,
                    ActionType = "Completed",
                    Duration = rent.Duration,
                    Hours = rent.Hours,
                    Seen = false,
                    AdminSeen = false,
                    Amount = rent.Amount,
                    AdvancePaid = rent.AdvancePaid,
                    RemainingAmount = rent.RemainingAmount,
                    PaymentMethod = rent.PaymentMethod,
                    PickupBranchId = rent.PickupBranchId
                };
                _context.RentalHistory!.Add(rentalhistory);

                var returnUser = await _userManager.FindByIdAsync(rent.UserId);
                var durationDisplay = rent.Duration > 0 ? $"{rent.Duration}d" : "";
                if (rent.Hours > 0) durationDisplay += (durationDisplay.Length > 0 ? " " : "") + $"{rent.Hours}h";
                await LogAudit("Returned", "Rent", id.ToString(), $"Car returned. Car: {car.ModelName}, User: {returnUser?.Email ?? rent.UserId}, Amount: {rent.Amount}, Duration: {durationDisplay}" + (lateFeeAmount > 0 ? $", Late fee: Rs. {Math.Round(lateFeeAmount, 2)}" : ""));

                _context.Rent.Remove(rent);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(ReturnedCar));
        }
        private bool RentExists(Guid id)
        {
            return (_context.Rent?.Any(e => e.Id == id)).GetValueOrDefault();
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