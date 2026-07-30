using HajurKoCarRental.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HajurKoCarRental.Areas.Identity.Data;
using HajurKoCarRental.Models;
using HajurKoCarRental.Services;
using System.Text.Json;
using System.Security.Claims;

namespace HajurKoCarRental.Controllers
{
    //RentalController class to handle rents
    [Authorize]
    public class RentController : Controller
    {
        private readonly HajurKoCarRentalContext _context;
        private readonly SignInManager<HajurKoCarRentalUser> _signInManager;
        private readonly UserManager<HajurKoCarRentalUser> _userManager;
        private readonly IWebHostEnvironment _webHost;
        private readonly FestivalService _festivalService;

        public RentController(HajurKoCarRentalContext context, SignInManager<HajurKoCarRentalUser> signInManager, UserManager<HajurKoCarRentalUser> userManager, IWebHostEnvironment webHost, FestivalService festivalService)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
            _webHost = webHost;
            _festivalService = festivalService;
        }
        
        // Redirects user to the landing or index view page of the rental request
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Admin accounts cannot rent cars.";
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            return View(currentUser);
        }

        // Asks user to complete profile at the time of renting if the profile is incomplete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<Object> UpdateUser(HajurKoCarRentalUser model)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(HttpContext.User);
                var currentUser = await _userManager.GetUserAsync(HttpContext.User);

                if (model.CitizenshipPhoto != null && model.CitizenshipPhoto.Length > 0)
                {
                    
                    if (model.CitizenshipPhoto.Length > 1572864)
                    {
                        TempData["ErrorMessage"] = "Citizenship photo must be less than or equal to 1.5 MB.!";
                        return RedirectToAction("Index", "Rent");

                    }

                    if (!model.CitizenshipPhoto.ContentType.Equals("image/png") && !model.CitizenshipPhoto.ContentType.Equals("application/pdf"))
                    {
                        TempData["ErrorMessage"] = "Citizenship photo must be in PNG or PDF format.!";
                        return RedirectToAction("Index", "Rent");

                    }

                    currentUser.CitizenshipPhoto = model.CitizenshipPhoto;
                }

                if (model.LicencePhoto != null && model.LicencePhoto.Length > 0)
                {
                  
                    if (model.LicencePhoto.Length > 1572864) 
                    {
                        TempData["ErrorMessage"] = "Citizenship photo must be less than or equal to 1.5 MB.!";
                        return RedirectToAction("Index", "Rent");
                    }

                    if (!model.LicencePhoto.ContentType.Equals("image/png") && !model.LicencePhoto.ContentType.Equals("application/pdf"))
                    {
                        TempData["ErrorMessage"] = "Lisence photo must be in PNG or PDF format.!";
                        return RedirectToAction("Index", "Rent");
                    }

                    currentUser.LicencePhoto = model.LicencePhoto;
                }

                if (model.ProfilePhoto != null && model.ProfilePhoto.Length > 0)
                {
                    if (model.ProfilePhoto.Length > 1572864) 
                    {
                        TempData["ErrorMessage"] = "Profile photo must be less than or equal to 1.5 MB.!";
                        return RedirectToAction("Index", "Rent");
                    }

                    if (!model.ProfilePhoto.ContentType.Equals("image/png"))
                    {
                       
                        TempData["ErrorMessage"] = "Profile photo must be in PNG or PDF format.!";
                        return RedirectToAction("Index", "Rent");
                    }

                    currentUser.ProfilePhoto = model.ProfilePhoto;
                }
                
                string uniqueFileName = GetProfilePhotoFileName(currentUser);

                string uniqueFileName1 = GetCitizenshipPhotoFileName(currentUser);

                string uniqueFileName2 = GetLicencePhotoFileName(currentUser);
               
                currentUser.Firstname = model.Firstname;
                currentUser.Lastname = model.Lastname;
                currentUser.Address = model.Address;
                currentUser.PhoneNumber = model.PhoneNumber;
                currentUser.ProfilePhotoUrl = uniqueFileName;
                currentUser.CitizenshipPhotoUrl = uniqueFileName1;
                currentUser.LicencePhotoUrl = uniqueFileName2;
                currentUser.IsProfileComplete = true;

                try
                {
                    var result = _context.Update(currentUser);
                    _context.SaveChanges();
                    return RedirectToAction("Index", "Home");

                }
                catch (Exception ex)
                {
                    return ex;
                }
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        // Checks if the profile is complete
        public async Task<IActionResult> CheckProfileStatus(Guid carId)
        {
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            if (currentUser.IsProfileComplete != null && currentUser.IsProfileComplete == true)
            {
                return RedirectToAction(nameof(RentDescription), new { carModel = carId });
            }
            else
            {
                return RedirectToAction(nameof(Index));
            }
        }

        //Copies the selected photo to the images folder of the wwwroot folder and returns the url to the image
        private string GetProfilePhotoFileName(HajurKoCarRentalUser user)
        {
            string uniqueFileName = null;

            if (user.ProfilePhoto != null)
            {
                string uploadsFolder = Path.Combine(_webHost.WebRootPath, "images");
                uniqueFileName = Guid.NewGuid().ToString() + "_" + user.ProfilePhoto.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    user.ProfilePhoto.CopyTo(fileStream);
                }
            }
            return uniqueFileName;
        }

        //Copies the selected photo to the images folder of the wwwroot folder and returns the url to the image
        private string GetCitizenshipPhotoFileName(HajurKoCarRentalUser user)
        {
            string uniqueFileName = null;

            if (user.CitizenshipPhoto != null)
            {
                string uploadsFolder = Path.Combine(_webHost.WebRootPath, "images");
                uniqueFileName = Guid.NewGuid().ToString() + "_" + user.CitizenshipPhoto.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    user.CitizenshipPhoto.CopyTo(fileStream);
                }
            }
            return uniqueFileName;
        }

        //Copies the selected photo to the images folder of the wwwroot folder and returns the url to the image
        private string GetLicencePhotoFileName(HajurKoCarRentalUser user)
        {
            string uniqueFileName = null;

            if (user.LicencePhoto != null)
            {
                string uploadsFolder = Path.Combine(_webHost.WebRootPath, "images");
                uniqueFileName = Guid.NewGuid().ToString() + "_" + user.LicencePhoto.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    user.LicencePhoto.CopyTo(fileStream);
                }
            }
            return uniqueFileName;
        }

        // Combines related models to return in the rent index view
        public class UserAndCar
        {
            public HajurKoCarRentalUser User { get; set; }
            public Car Car { get; set; }
            public bool IsStaff { get; set; }
            public DateTime RentalDate { get; set; }
            public decimal FestivalMultiplier { get; set; } = 1.0m;
            public string ActiveFestivalName { get; set; }
        }
        private Car GetCarById(Guid id)
        {
            var car = _context.Car.Include(c => c.CarBrand).FirstOrDefault(c => c.Id == id);
            return car;
        }

        // Returns description of the rent
        public async Task<IActionResult> RentDescription(Guid carModel)
        {
            if (User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Admin accounts cannot rent cars.";
                return RedirectToAction("Index", "Home");
            }
            var car = _context.Car!
                .Include(c => c.CarBrand)
                .Include(c => c.CarBranches)
                    .ThenInclude(cb => cb.Branch)
                .FirstOrDefault(c => c.Id == carModel);
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            var festivalMultiplier = await _festivalService.GetFestivalMultiplierAsync(DateTime.Today);
            var activeFestival = await _festivalService.GetActiveFestivalAsync(DateTime.Today);

            UserAndCar urv = new UserAndCar()
            {
                User = currentUser,
                Car = car,
                IsStaff = false,
                RentalDate = default,
                FestivalMultiplier = festivalMultiplier,
                ActiveFestivalName = activeFestival?.Name,
            };
            return View(urv);
        }

        // Submits rental requests
        public async Task<IActionResult> SubmitRequest(Guid carId, DateTime rentalStart, int duration = 1, int hours = 0, string discountCode = null, Guid? pickupBranch = null, double? pickupLat = null, double? pickupLng = null)
        {
            if (User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Admin accounts cannot rent cars.";
                return RedirectToAction("Index", "Home");
            }
            if (rentalStart < DateTime.Now.AddMinutes(-10))
            {
                TempData["ErrorMessage"] = "You cannot rent a car for a past date.";
                return RedirectToAction("Index", "Home");
            }
            var validationError = RentalPricingHelper.ValidateDuration(duration, hours);
            if (validationError != null)
            {
                TempData["ErrorMessage"] = validationError;
                return RedirectToAction("Index", "Home");
            }

            Car car = GetCarById(carId);
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);

            var hasUnresolvedLateFee = await _context.LateFees.AnyAsync(f => f.UserId == currentUser.Id && f.Status == "Unpaid");
            if (hasUnresolvedLateFee)
            {
                TempData["ErrorMessage"] = "You have an outstanding late fee. Please resolve it before renting again.";
                return RedirectToAction("MyFees", "LateFee");
            }

            // Apply festival pricing
            var festivalMultiplier = await _festivalService.GetFestivalMultiplierAsync(rentalStart);
            double dailyRate = car.Price * (double)festivalMultiplier;
            double baseRate = (dailyRate * duration) + RentalPricingHelper.GetHoursCost(dailyRate, hours);
            double totalPrice = baseRate;

            // Apply retention offer or long-term rental discount (mutually exclusive)
            bool discountApplied = false;
            string discountLabel = "";
            bool codeIsValid = false;
            if (!string.IsNullOrWhiteSpace(discountCode))
            {
                var offer = await _context.RetentionOffer!
                    .FirstOrDefaultAsync(o => o.DiscountCode == discountCode && o.UserId == currentUser.Id && o.UsedAt == null);
                if (offer != null)
                {
                    double retDiscount = (offer.DiscountPercent * baseRate) / 100;
                    totalPrice = baseRate - retDiscount;
                    offer.UsedAt = DateTime.Now;
                    _context.Update(offer);
                    discountApplied = true;
                    codeIsValid = true;
                }
                else
                {
                    TempData["ErrorMessage"] = "Invalid or expired discount code.";
                }
            }
            if (!codeIsValid)
            {
                double longTermDiscountPct = 0;
                if (duration >= 21) longTermDiscountPct = 15;
                else if (duration >= 11) longTermDiscountPct = 10;

                if (longTermDiscountPct > 0)
                {
                    var discountAmount = (longTermDiscountPct * totalPrice) / 100;
                    totalPrice = totalPrice - discountAmount;
                    discountLabel = longTermDiscountPct + "% long-term discount applied";
                }
            }

            if (totalPrice < 0) totalPrice = 0;

            Rent rent = new Rent
            {
                CarId = carId,
                UserId = currentUser.Id,
                PaymentMethod = "Cash",
                Amount = totalPrice,
                AdvancePaid = Math.Round(totalPrice * 0.5, 2),
                RemainingAmount = Math.Round(totalPrice * 0.5, 2),
                RequestDate = DateTime.Now,
                RentalDate = rentalStart,
                Verified = false,
                Status = "Pending",
                Duration = duration,
                Hours = hours,
                AdminSeen = false,
                CustomerSeen = true,
                DiscountCode = discountCode,
                PickupBranchId = pickupBranch,
                PickupLat = pickupLat,
                PickupLng = pickupLng
            };
            string msg = "Rent Request Sent successfully.";
            if (discountApplied) msg += " Retention offer discount applied.";
            if (!discountApplied && !string.IsNullOrWhiteSpace(discountLabel)) msg += " " + discountLabel;
            TempData["SuccessMessage"] = msg;
            _context.Rent.Add(rent);
            _context.SaveChanges();

            int? count = 0;
            if (car.BookedCount != null)
            {
                count = car.BookedCount;
            }
            car.BookedCount = count + 1;
            _context.Update(car);
            _context.SaveChanges();
            return RedirectToAction("Index", "Home");
        }

        // It manages payment from khalti
        public async Task<IActionResult> KhaltiPayment(Guid carId, DateTime rentalStart, int duration = 1, int hours = 0, string discountCode = null, Guid? pickupBranch = null, double? pickupLat = null, double? pickupLng = null)
        {
            if (User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Admin accounts cannot rent cars.";
                return RedirectToAction("Index", "Home");
            }
            if (rentalStart < DateTime.Now.AddMinutes(-10))
            {
                TempData["ErrorMessage"] = "You cannot rent a car for a past date.";
                return RedirectToAction("Index", "Home");
            }
            var validationError = RentalPricingHelper.ValidateDuration(duration, hours);
            if (validationError != null)
            {
                TempData["ErrorMessage"] = validationError;
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            var hasUnresolvedLateFee = await _context.LateFees.AnyAsync(f => f.UserId == currentUser.Id && f.Status == "Unpaid");
            if (hasUnresolvedLateFee)
            {
                TempData["ErrorMessage"] = "You have an outstanding late fee. Please resolve it before renting again.";
                return RedirectToAction("MyFees", "LateFee");
            }
            Car car = GetCarById(carId);
            var userName = currentUser.Firstname + " " + currentUser.Lastname;
            var userEmail = currentUser.Email;
            var userPhone = currentUser.PhoneNumber;

            double dailyRate = car.Price * (double)await _festivalService.GetFestivalMultiplierAsync(rentalStart);
            double baseRate = (dailyRate * duration) + RentalPricingHelper.GetHoursCost(dailyRate, hours);
            double totalPrice = baseRate;

            // Apply retention offer or long-term rental discount (mutually exclusive)
            bool codeIsValid = false;
            if (!string.IsNullOrWhiteSpace(discountCode))
            {
                var offer = await _context.RetentionOffer!
                    .FirstOrDefaultAsync(o => o.DiscountCode == discountCode && o.UserId == currentUser.Id && o.UsedAt == null);
                if (offer != null)
                {
                    double retDiscount = (offer.DiscountPercent * baseRate) / 100;
                    totalPrice = baseRate - retDiscount;
                    codeIsValid = true;
                }
            }
            if (!codeIsValid)
            {
                double longTermDiscountPct = 0;
                if (duration >= 21) longTermDiscountPct = 15;
                else if (duration >= 11) longTermDiscountPct = 10;

                if (longTermDiscountPct > 0)
                {
                    var discountAmount = (longTermDiscountPct * totalPrice) / 100;
                    totalPrice = totalPrice - discountAmount;
                }
            }

            if (totalPrice <= 0)
            {
                TempData["ErrorMessage"] = "This discount makes the total zero. Please use Cash payment instead.";
                return RedirectToAction("CarDetails", "Home", new { id = carId });
            }

            double advanceAmount = Math.Round(totalPrice * 0.5, 2);
            double fullTotalPrice = totalPrice;
            advanceAmount *= 100;
            string returnUrl = Url.Action(nameof(SuccessPayment), "Rent", new { rentalStart, duration, hours, discountCode, pickupBranch, pickupLat, pickupLng, totalPrice = fullTotalPrice.ToString(System.Globalization.CultureInfo.InvariantCulture) }, Request.Scheme)!;
            var payload = new
            {
                amount = advanceAmount,
                purchase_order_id = carId,
                purchase_order_name = "Car",
                return_url = returnUrl,
                website_url = "http://localhost:8000/",
                payment_preference = new[] { "KHALTI" },
                customer_info = new
                {
                    name = userName,
                    email = userEmail,
                    phone = userPhone,
                }
            };

            string url = "https://a.khalti.com/api/v2/epayment/initiate/";
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Key 9cf26609a75a447ba55db50b47b544f1");

            var response = await client.PostAsJsonAsync(url, payload);
            response.EnsureSuccessStatusCode();
            string responseContent = await response.Content.ReadAsStringAsync();
            dynamic responseData = JsonSerializer.Deserialize<dynamic>(responseContent)!;
            var paymentUrl = responseData.GetProperty("payment_url").GetString();

            return Redirect(paymentUrl);
        }

        // Executes if the khalti payment is successful
        [HttpGet]
        public async Task<IActionResult> SuccessPayment()
        {
            string message = HttpContext.Request.Query["message"];
            string amount = HttpContext.Request.Query["amount"];

            decimal totalAmount = decimal.Parse(amount) / 100;
            int advancePaid = Convert.ToInt32(totalAmount);
            string CarId = HttpContext.Request.Query["purchase_order_id"];
            string rentalStartString = HttpContext.Request.Query["rentalStart"];
            int idx = rentalStartString.IndexOf('?');
            string urlWithoutQueryParams = idx >= 0 ? rentalStartString.Substring(0, idx) : rentalStartString;
            DateTime rentalStart = DateTime.Parse(urlWithoutQueryParams);

            string durationString = HttpContext.Request.Query["duration"];
            int dIdx = durationString.IndexOf('?');
            string cleanDurationString = dIdx >= 0 ? durationString.Substring(0, dIdx) : durationString;
            int duration = int.TryParse(cleanDurationString, out var d) ? d : 1;

            string hoursString = HttpContext.Request.Query["hours"];
            int hIdx = hoursString.IndexOf('?');
            string cleanHoursString = hIdx >= 0 ? hoursString.Substring(0, hIdx) : hoursString;
            int hours = int.TryParse(cleanHoursString, out var h) ? h : 0;

            string totalPriceString = HttpContext.Request.Query["totalPrice"];
            int tpIdx = totalPriceString.IndexOf('?');
            string cleanTotalPriceString = tpIdx >= 0 ? totalPriceString.Substring(0, tpIdx) : totalPriceString;
            double fullTotalPrice = double.TryParse(cleanTotalPriceString, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var tp) ? tp : advancePaid * 2;

            Guid carRentID = Guid.Parse(CarId);
            Car car = GetCarById(carRentID);
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return NotFound();
            }

            string discountCode = HttpContext.Request.Query["discountCode"];
            string pickupBranchStr = HttpContext.Request.Query["pickupBranch"];
            Guid? pickupBranch = null;
            if (Guid.TryParse(pickupBranchStr, out var pb))
                pickupBranch = pb;

            string pickupLatStr = HttpContext.Request.Query["pickupLat"];
            string pickupLngStr = HttpContext.Request.Query["pickupLng"];
            double? pickupLat = null;
            double? pickupLng = null;
            if (double.TryParse(pickupLatStr, out var pl)) pickupLat = pl;
            if (double.TryParse(pickupLngStr, out var pn)) pickupLng = pn;

            if (string.IsNullOrEmpty(message))
            {
                TempData["SuccessMessage"] = "Payment successful! Advance (50%) paid.";
                Rent rent = new Rent
                {
                    CarId = carRentID,
                    UserId = userId,
                    PaymentMethod = "Khalti",
                    Amount = fullTotalPrice,
                    AdvancePaid = advancePaid,
                    RemainingAmount = fullTotalPrice - advancePaid,
                    Verified = false,
                    RequestDate = DateTime.Now,
                    RentalDate = rentalStart,
                    Status = "Pending",
                    Duration = duration,
                    Hours = hours,
                    AdminSeen = false,
                    CustomerSeen = true,
                    DiscountCode = discountCode,
                    PickupBranchId = pickupBranch,
                    PickupLat = pickupLat,
                    PickupLng = pickupLng
                };
                _context.Rent!.Add(rent);
                _context.SaveChanges();
                int? count = 0;
                if (car.BookedCount != null)
                {
                    count = car.BookedCount;
                }
                car.BookedCount = count + 1;
                _context.Update(car);
                _context.SaveChanges();

                // Mark retention offer as used after successful payment
                if (!string.IsNullOrWhiteSpace(discountCode))
                {
                    var offer = await _context.RetentionOffer!
                        .FirstOrDefaultAsync(o => o.DiscountCode == discountCode && o.UserId == userId && o.UsedAt == null);
                    if (offer != null)
                    {
                        offer.UsedAt = DateTime.Now;
                        _context.Update(offer);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        TempData["WarningMessage"] = "The discount code you entered is invalid or expired. Payment was processed without discount.";
                    }
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Payment failed!";
            }
            return RedirectToAction("Index","Home");
        }

        // Initiates Khalti payment for remaining balance on an active rental
        public async Task<IActionResult> PayRemaining(Guid rentId)
        {
            var rent = await _context.Rent!.Include(r => r.Car).FirstOrDefaultAsync(r => r.Id == rentId);
            if (rent == null)
            {
                TempData["ErrorMessage"] = "Rental not found.";
                return RedirectToAction("Index", "Home");
            }

            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (rent.UserId != userId)
            {
                TempData["ErrorMessage"] = "You can only pay remaining balance for your own rentals.";
                return RedirectToAction("Index", "Home");
            }

            if (rent.RemainingAmount <= 0)
            {
                TempData["SuccessMessage"] = "No remaining balance to pay.";
                return RedirectToAction("Index", "CustomerDashboard");
            }

            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            var userName = currentUser.Firstname + " " + currentUser.Lastname;
            var userEmail = currentUser.Email;
            var userPhone = currentUser.PhoneNumber;

            double remainingPaisa = rent.RemainingAmount * 100;
            string returnUrl = Url.Action(nameof(SuccessPayRemaining), "Rent", new { rentId }, Request.Scheme)!;
            var payload = new
            {
                amount = remainingPaisa,
                purchase_order_id = rentId,
                purchase_order_name = "Remaining Balance",
                return_url = returnUrl,
                website_url = "http://localhost:8000/",
                payment_preference = new[] { "KHALTI" },
                customer_info = new
                {
                    name = userName,
                    email = userEmail,
                    phone = userPhone,
                }
            };

            string url = "https://a.khalti.com/api/v2/epayment/initiate/";
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Key 9cf26609a75a447ba55db50b47b544f1");

            var response = await client.PostAsJsonAsync(url, payload);
            response.EnsureSuccessStatusCode();
            string responseContent = await response.Content.ReadAsStringAsync();
            dynamic responseData = JsonSerializer.Deserialize<dynamic>(responseContent)!;
            var paymentUrl = responseData.GetProperty("payment_url").GetString();

            return Redirect(paymentUrl);
        }

        // Executes if the remaining balance payment is successful
        [HttpGet]
        public async Task<IActionResult> SuccessPayRemaining()
        {
            string message = HttpContext.Request.Query["message"];
            string rentIdStr = HttpContext.Request.Query["purchase_order_id"];

            if (string.IsNullOrEmpty(message) && !string.IsNullOrEmpty(rentIdStr))
            {
                Guid rentId = Guid.Parse(rentIdStr);
                var rent = await _context.Rent!.FirstOrDefaultAsync(r => r.Id == rentId);
                if (rent != null)
                {
                    rent.RemainingAmount = 0;
                    _context.Update(rent);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Remaining balance paid successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Rental record not found.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Payment failed!";
            }
            return RedirectToAction("Index", "CustomerDashboard");
        }


    }
}