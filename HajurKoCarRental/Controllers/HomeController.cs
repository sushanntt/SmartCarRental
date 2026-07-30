using HajurKoCarRental.Data;
using HajurKoCarRental.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using HajurKoCarRental.Areas.Identity.Data;
using Microsoft.AspNetCore.Hosting;
using static HajurKoCarRental.Controllers.RentController;
using HajurKoCarRental.Services;

namespace HajurKoCarRental.Controllers
{
    // HomeController class to manage the home page or the landing page
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HajurKoCarRentalContext _context;
        private readonly UserManager<HajurKoCarRentalUser> _userManager;
        private readonly FestivalService _festivalService;
        private readonly RecommendationService _recommendationService;
        private readonly IWebHostEnvironment _webHost;
        private readonly IConfiguration _configuration;

        public HomeController(
            ILogger<HomeController> logger,
            HajurKoCarRentalContext context,
            UserManager<HajurKoCarRentalUser> userManager,
            FestivalService festivalService,
            RecommendationService recommendationService,
            IWebHostEnvironment webHost,
            IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _festivalService = festivalService;
            _recommendationService = recommendationService;
            _webHost = webHost;
            _configuration = configuration;
        }

        // Combines the car, carbrands, and offer model to return combined data on the home page
        public class CarViewModel
        {
            public List<Car> Car { get; set; }
            public List<CarBrand> CarBrands { get; set; }
            public List<Offer> Offer { get; set; }
            public int AvailableCarsCount { get; set; }
            public int RentalHistoryCount { get; set; }
            public int ActiveOffersCount { get; set; }
            public List<object> AvailableCarsByBrand { get; set; }
            public int CustomerCount { get; set; }
            public int StaffCount { get; set; }
            // Festival price multiplier active right now (1.0 = no festival)
            public decimal FestivalMultiplier { get; set; } = 1.0m;
            public string ActiveFestivalName { get; set; }
            // Collaborative Filtering recommendations
            public List<Car> RecommendedCars { get; set; } = new List<Car>();
        }

        // Redirects user to the home page with the cars and offers data
        public async Task<IActionResult> Index(string search)
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Analytics");
            }
            if (User.IsInRole("Customer"))
            {
                return RedirectToAction("Index", "CustomerDashboard");
            }
            // Get current festival multiplier (applies to today's date)
            var festivalMultiplier = await _festivalService.GetFestivalMultiplierAsync(DateTime.Today);
            var activeFestival = await _festivalService.GetActiveFestivalAsync(DateTime.Today);

            if (string.IsNullOrEmpty(search))
            {
                var car = _context.Car!.Include(c => c.CarBrand).Where(c => c.Available == true).ToList();
                var carBrands = _context.CarBrand!.ToList();
                var offer = _context.Offer!.ToList();
                foreach (var offers in offer)
                {
                    if (offers.ValidityPeroid < DateTime.Now)
                    {
                        offers.Available = false;
                    }
                }
                _context.SaveChanges();

                var currentUser = await _userManager.GetUserAsync(HttpContext.User);
                int rentalHistoryCount = 0;
                if (currentUser != null)
                {
                    rentalHistoryCount = _context.RentalHistory!.Count(r => r.UserId == currentUser.Id);
                }

                var customers = await _userManager.GetUsersInRoleAsync("Customer");

                var availableCarsByBrand = _context.Car!
                    .Include(c => c.CarBrand)
                    .Where(c => c.Available == true && (c.IsCarBooked == false || c.IsCarBooked == null))
                    .GroupBy(c => c.CarBrand.BrandName)
                    .Select(g => new { BrandName = g.Key, CarCount = g.Count() })
                    .ToList<object>();

                // Get recommendations for logged-in customer
                List<Car> recommendedCars = new List<Car>();
                if (currentUser != null && User.IsInRole("Customer"))
                {
                    recommendedCars = await _recommendationService.GetRecommendedCarsAsync(currentUser.Id);
                }

                var viewModel = new CarViewModel
                {
                    Car = car,
                    CarBrands = carBrands,
                    Offer = offer,
                    AvailableCarsCount = car.Count(c => c.IsCarBooked == false || c.IsCarBooked == null),
                    RentalHistoryCount = rentalHistoryCount,
                    ActiveOffersCount = offer.Count(o => o.Available == true && o.ValidityPeroid >= DateTime.Now),
                    AvailableCarsByBrand = availableCarsByBrand,
                    CustomerCount = customers.Count,
                    StaffCount = 0,
                    FestivalMultiplier = festivalMultiplier,
                    ActiveFestivalName = activeFestival?.Name,
                    RecommendedCars = recommendedCars
                };
                return View(viewModel);
            }
            else
            {
                var filteredCars = _context.Car!.Where(c => c.Available == true && (c.ModelName.Contains(search) || c.ModelYear.ToString().Contains(search))).ToList();
                var viewModel = new CarViewModel
                {
                    Car = filteredCars,
                    CarBrands = _context.CarBrand!.ToList(),
                    Offer = _context.Offer!.ToList(),
                    FestivalMultiplier = festivalMultiplier,
                    ActiveFestivalName = activeFestival?.Name
                };
                return View(viewModel);
            }
        }

        // Filters cars according to brand
        [HttpGet]
        public async Task<IActionResult> FilterCarList(Guid brandId)
        {
            var festivalMultiplier = await _festivalService.GetFestivalMultiplierAsync(DateTime.Today);
            var activeFestival = await _festivalService.GetActiveFestivalAsync(DateTime.Today);
            var filteredData = await _context.Car!.Include(c => c.CarBrand).Where(c => c.Available == true && c.BrandId == brandId).ToListAsync();
            var carBrands = _context.CarBrand!.ToList();
            var viewModel = new CarViewModel
            {
                Car = filteredData,
                CarBrands = carBrands,
                Offer = _context.Offer!.ToList(),
                FestivalMultiplier = festivalMultiplier,
                ActiveFestivalName = activeFestival?.Name
            };
            return View("Index", viewModel);
        }

        // Returns all the cars (available and unavailable)
        public async Task<IActionResult> CarList(string search)
        {
            var festivalMultiplier = await _festivalService.GetFestivalMultiplierAsync(DateTime.Today);
            var activeFestival = await _festivalService.GetActiveFestivalAsync(DateTime.Today);

            if (string.IsNullOrEmpty(search))
            {
                var car = _context.Car!.Where(c => c.Available == true).ToList();
                var carBrands = _context.CarBrand!.ToList();
                var viewModel = new CarViewModel
                {
                    Car = car,
                    CarBrands = carBrands,
                    FestivalMultiplier = festivalMultiplier,
                    ActiveFestivalName = activeFestival?.Name
                };
                return View(viewModel);
            }
            else
            {
                var filteredCars = _context.Car!.Include(c => c.CarBrand).Where(c => c.Available == true && (c.ModelName.Contains(search) || c.ModelYear.ToString().Contains(search) || c.CarBrand.BrandName.Contains(search))).ToList();
                var viewModel = new CarViewModel
                {
                    Car = filteredCars,
                    CarBrands = _context.CarBrand!.ToList(),
                    FestivalMultiplier = festivalMultiplier,
                    ActiveFestivalName = activeFestival?.Name
                };
                return View(viewModel);
            }
        }

        // Returns search result from the search box in home page
        [HttpGet]
        public async Task<IActionResult> AllFilterCarList(Guid brandId)
        {
            var festivalMultiplier = await _festivalService.GetFestivalMultiplierAsync(DateTime.Today);
            var activeFestival = await _festivalService.GetActiveFestivalAsync(DateTime.Today);

            var filteredData = await _context.Car!.Where(c => c.Available == true && c.BrandId == brandId).ToListAsync();
            var carBrands = _context.CarBrand!.ToList();
            var viewModel = new CarViewModel
            {
                Car = filteredData,
                CarBrands = carBrands,
                FestivalMultiplier = festivalMultiplier,
                ActiveFestivalName = activeFestival?.Name
            };
            return View("CarList", viewModel);
        }
        
        // About page
        public IActionResult About()
        {
            return View();
        }

        // Contact page
        public IActionResult Contact()
        {
            return View();
        }

        // Sends mail to the admin
        [HttpPost]
        public async Task<IActionResult> SendMail(string name, string email, string message)
        {
            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] = "Please fill in your name, email, and message.";
                return RedirectToAction("Contact");
            }

            var from = _configuration["Email:FromAddress"];
            var to = _configuration["Email:AdminAddress"] ?? from;
            var password = _configuration["Email:Password"];

            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(password))
            {
                TempData["ErrorMessage"] = "Email is not configured. Set Email:Password in appsettings.Development.json, then restart the app.";
                return RedirectToAction("Contact");
            }

            try
            {
                var body =
                    $"You received a new message from the Contact Us page.\n\n" +
                    $"Name: {name.Trim()}\n" +
                    $"Email: {email.Trim()}\n\n" +
                    $"Message:\n{message.Trim()}";

                await GmailSender.SendAsync(
                    from,
                    password,
                    to!,
                    $"Contact form: {name.Trim()}",
                    body,
                    email.Trim());

                TempData["SuccessMessage"] = "Email sent successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Contact form email failed.");
                TempData["ErrorMessage"] = "Could not send email. Confirm the Gmail App Password in appsettings.Development.json matches sushaantthapa000@gmail.com, then restart the app.";
            }

            return RedirectToAction("Contact");
        }

        // Compare 2-3 cars side-by-side
        public async Task<IActionResult> Compare(List<Guid> ids)
        {
            if (ids == null || ids.Count < 2 || ids.Count > 3)
            {
                TempData["ErrorMessage"] = "Please select 2 to 3 cars to compare.";
                return RedirectToAction(nameof(CarList));
            }

            var cars = await _context.Car!
                .Include(c => c.CarBrand)
                .Where(c => ids.Contains(c.Id))
                .ToListAsync();

            if (cars.Count < 2)
            {
                TempData["ErrorMessage"] = "Selected cars not found.";
                return RedirectToAction(nameof(CarList));
            }

            var festivalMultiplier = await _festivalService.GetFestivalMultiplierAsync(DateTime.Today);
            ViewBag.FestivalMultiplier = festivalMultiplier;

            return View(cars);
        }

        // Returns user to the car details page where user can see details of the car
        public async Task<IActionResult> CarDetails(Guid id)
        {
            var festivalMultiplier = await _festivalService.GetFestivalMultiplierAsync(DateTime.Today);
            var activeFestival = await _festivalService.GetActiveFestivalAsync(DateTime.Today);
            ViewBag.FestivalMultiplier = festivalMultiplier;
            ViewBag.ActiveFestivalName = activeFestival?.Name;

            Car car = GetCarById(id);
            if (car == null) return NotFound();

            // Load CarBranches for display
            car.CarBranches = _context.CarBranches!
                .Include(cb => cb.Branch)
                .Where(cb => cb.CarId == id)
                .ToList();

            return View(car);
        }

        // Helper function --> Returns car based on its id
		private Car GetCarById(Guid id)
		{
			var car = _context.Car!.Include(c => c.CarBrand).FirstOrDefault(c => c.Id == id);
			return car;
		}

        // Helper function --> Returns offers based on its id
		private Offer GetOfferById(Guid id)
		{
			var offer = _context.Offer!.FirstOrDefault(o => o.Id == id);
			return offer;
		}

        // Helper function --> Returns offers details
		public IActionResult OfferDetails(Guid id)
        {
			var offer = _context.Offer!
                .Include(o => o.OfferBranches)
                    .ThenInclude(ob => ob.Branch)
                .FirstOrDefault(o => o.Id == id);
            if (offer == null) return NotFound();
			return View(offer);
		}

        // This function executes before the payment method and ensures that user can book the car
        [Authorize]
        public async Task<IActionResult> BeforeKhalti(Guid carId)
        {
            if (User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Admin accounts cannot rent cars.";
                return RedirectToAction("Index", "Home");
            }
            var car = _context.Car!.Include(c => c.CarBrand).FirstOrDefault(c => c.Id == carId);
            if (car!.IsCarBooked == null || car.IsCarBooked == false)
            {
                var currentUser = await _userManager.GetUserAsync(HttpContext.User);
                var hasUnresolvedLateFee = await _context.LateFees.AnyAsync(f => f.UserId == currentUser.Id && f.Status == "Unpaid");
                if (hasUnresolvedLateFee)
                {
                    TempData["ErrorMessage"] = "You have an outstanding late fee. Please resolve it before renting again.";
                    return RedirectToAction("MyFees", "LateFee");
                }
                if (currentUser.CanBook == true)
                {
                    if (currentUser.IsProfileComplete != null && currentUser.IsProfileComplete == true)
                    {
                        return RedirectToAction("RentDescription", "Rent", new { carModel = carId });
                    }
                    else
                    {
                        return RedirectToAction("Index", "Rent");
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Pay the damage charge to book new car!";
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        // Checks if the user has complete profile
        [Authorize]
        public async Task<IActionResult> OfferCheck(Guid offerId)
        {
            if (User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Admin accounts cannot rent cars.";
                return RedirectToAction("Index", "Home");
            }
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            var hasUnresolvedLateFee = await _context.LateFees.AnyAsync(f => f.UserId == currentUser.Id && f.Status == "Unpaid");
            if (hasUnresolvedLateFee)
            {
                TempData["ErrorMessage"] = "You have an outstanding late fee. Please resolve it before renting again.";
                return RedirectToAction("MyFees", "LateFee");
            }
            if (currentUser.CanBook == true)
            {
                if (currentUser.IsProfileComplete != null && currentUser.IsProfileComplete == true)
                {
                    return RedirectToAction("RentDescription", "Home", new { offerModel = offerId });
                }
                else
                {
                    return RedirectToAction("Index", "Rent");
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Pay the damage charge to book new car!";
                return RedirectToAction("Index", "Home");
            }
        }

        // Combines user and offer model
        public class UserAndOffer
        {
            public HajurKoCarRentalUser User { get; set; }
            public Offer Offer { get; set; }
        }

        // Returns details of current user and selected offer
        public async Task<IActionResult> RentDescription(Guid offerModel)
        {
            if (User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Admin accounts cannot rent cars.";
                return RedirectToAction("Index", "Home");
            }
            var offer = _context.Offer!.Include(o => o.OfferBranches).ThenInclude(ob => ob.Branch).FirstOrDefault(o => o.Id == offerModel);
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            var roles = await _userManager.GetRolesAsync(currentUser);
            var roleName = string.Join(", ", roles);

            UserAndOffer urv = new UserAndOffer()
            {
                User = currentUser,
                Offer = offer,
            };
            return View(urv);
        }

        // Submits the rent request
        public async Task<IActionResult> SubmitRequest(Guid carId, DateTime rentalStart, int duration = 1, Guid? pickupBranch = null, double? pickupLat = null, double? pickupLng = null)
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
            if (duration < 1 || duration > 30)
            {
                TempData["ErrorMessage"] = "Duration must be between 1 and 30 days.";
                return RedirectToAction("Index", "Home");
            }

            Offer offer = GetOfferById(carId);
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            var roles = await _userManager.GetRolesAsync(currentUser);
            var roleName = string.Join(", ", roles);

            var hasUnresolvedLateFee = await _context.LateFees.AnyAsync(f => f.UserId == currentUser.Id && f.Status == "Unpaid");
            if (hasUnresolvedLateFee)
            {
                TempData["ErrorMessage"] = "You have an outstanding late fee. Please resolve it before renting again.";
                return RedirectToAction("MyFees", "LateFee");
            }

            double totalPrice = offer.Price * duration;
            var discount = offer.DiscountPercentage;
            var discountAmount = (discount * totalPrice) / 100;
            totalPrice = totalPrice - discountAmount;

            RentOffer rentoffer = new RentOffer
            {
                OfferId = carId,
                UserId = currentUser.Id,
                PaymentMethod = "Cash",
                Amount = totalPrice,
                AdvancePaid = Math.Round(totalPrice * 0.5, 2),
                RemainingAmount = Math.Round(totalPrice * 0.5, 2),
                Verified = false,
                RentalDate = rentalStart,
                Duration = duration,
                AdminSeen = false,
                CustomerSeen = true,
                PickupBranchId = pickupBranch,
                PickupLat = pickupLat,
                PickupLng = pickupLng,
                Status = "Pending",
                RequestDate = DateTime.Now
            };
            TempData["SuccessMessage"] = "Rent Request Sent successfully.";
            _context.RentOffer!.Add(rentoffer);
            _context.SaveChanges();

            offer.IsBooked= true;
            _context.Update(offer);
            _context.SaveChanges();
            return RedirectToAction("Index", "Home");
        }

        // Khalti payment
        public async Task<IActionResult> KhaltiPayment(Guid carId, DateTime rentalStart, int duration = 1, Guid? pickupBranch = null, double? pickupLat = null, double? pickupLng = null)
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
            if (duration < 1 || duration > 30)
            {
                TempData["ErrorMessage"] = "Duration must be between 1 and 30 days.";
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            var roles = await _userManager.GetRolesAsync(currentUser);
            var roleName = string.Join(", ", roles);

            var hasUnresolvedLateFee = await _context.LateFees.AnyAsync(f => f.UserId == currentUser.Id && f.Status == "Unpaid");
            if (hasUnresolvedLateFee)
            {
                TempData["ErrorMessage"] = "You have an outstanding late fee. Please resolve it before renting again.";
                return RedirectToAction("MyFees", "LateFee");
            }

            Offer offer = GetOfferById(carId);
            double totalPrice = offer.Price * duration;
            var discount = offer.DiscountPercentage;
            var discountAmount = (discount * totalPrice) / 100;
            totalPrice = totalPrice - discountAmount;

            double advanceAmount = Math.Round(totalPrice * 0.5, 2);
            double fullTotalPrice = totalPrice;
            advanceAmount *= 100;
            var userName = currentUser.Firstname + " " + currentUser.Lastname;
            var userEmail = currentUser.Email;
            var userPhone = currentUser.PhoneNumber;
            string returnUrl = Url.Action(nameof(SuccessPayment), "Home", new { rentalStart, duration, pickupBranch, pickupLat, pickupLng, totalPrice = fullTotalPrice.ToString(System.Globalization.CultureInfo.InvariantCulture) }, Request.Scheme)!;
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

        // This function executes if the khalti pyament is successful and if yes saves the payment information to the database
        [HttpGet]
        public IActionResult SuccessPayment()
        {
            string message = HttpContext.Request.Query["message"];
            string amount = HttpContext.Request.Query["amount"];

            decimal totalAmount = decimal.Parse(amount) / 100;
            int advancePaid = Convert.ToInt32(totalAmount);
            string CarId = HttpContext.Request.Query["purchase_order_id"];
            Guid carRentID = Guid.Parse(CarId);
            string rentalStartString = HttpContext.Request.Query["rentalStart"];
            int idx = rentalStartString.IndexOf('?');
            string urlWithoutQueryParams = idx >= 0 ? rentalStartString.Substring(0, idx) : rentalStartString;
            DateTime rentalStart = DateTime.Parse(urlWithoutQueryParams);

            string durationString = HttpContext.Request.Query["duration"];
            int dIdx = durationString.IndexOf('?');
            string cleanDurationString = dIdx >= 0 ? durationString.Substring(0, dIdx) : durationString;
            int duration = int.TryParse(cleanDurationString, out var d) ? d : 1;

            string totalPriceString = HttpContext.Request.Query["totalPrice"];
            int tpIdx = totalPriceString.IndexOf('?');
            string cleanTotalPriceString = tpIdx >= 0 ? totalPriceString.Substring(0, tpIdx) : totalPriceString;
            double fullTotalPrice = double.TryParse(cleanTotalPriceString, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var tp) ? tp : advancePaid * 2;

            string pickupBranchStr = HttpContext.Request.Query["pickupBranch"];
            Guid? pickupBranch = Guid.TryParse(pickupBranchStr, out var pb) ? pb : null;
            string pickupLatStr = HttpContext.Request.Query["pickupLat"];
            double? pickupLat = double.TryParse(pickupLatStr, out var lat) ? lat : null;
            string pickupLngStr = HttpContext.Request.Query["pickupLng"];
            double? pickupLng = double.TryParse(pickupLngStr, out var lng) ? lng : null;

            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(message))
            {
                TempData["SuccessMessage"] = "Payment successful! Advance (50%) paid.";
                RentOffer rentoffer = new RentOffer
                {
                    OfferId = carRentID,
                    UserId = userId,
                    PaymentMethod = "Khalti",
                    Amount = fullTotalPrice,
                    AdvancePaid = advancePaid,
                    RemainingAmount = fullTotalPrice - advancePaid,
                    Verified = false,
                    RentalDate = rentalStart,
                    Duration = duration,
                    AdminSeen = false,
                    CustomerSeen = true,
                    PickupBranchId = pickupBranch,
                    PickupLat = pickupLat,
                    PickupLng = pickupLng,
                    Status = "Pending",
                    RequestDate = DateTime.Now
                };
                _context.RentOffer!.Add(rentoffer);
                _context.SaveChanges();
            }
            else
            {
                TempData["ErrorMessage"] = "Payment failed!";
            }
            return RedirectToAction("Index", "Home");
        }

        // Lists all the offers and returns data
        public IActionResult SpecialOffers()
        {
            var offer = _context.Offer!.Include(o => o.OfferBranches).ThenInclude(ob => ob.Branch).ToList();
            return View(offer);
        }

        // Handles errors that can occur
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}