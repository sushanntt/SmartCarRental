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
using System.Security.Claims;
using static HajurKoCarRental.Controllers.HomeController;
using HajurKoCarRental.Areas.Identity.Data;



namespace HajurKoCarRental.Controllers
{
    // PaymentController class for payment
    public class PaymentController : Controller
    {
        private readonly HajurKoCarRentalContext _context;
        private readonly UserManager<HajurKoCarRentalUser> _userManager;
        public PaymentController(HajurKoCarRentalContext context, UserManager<HajurKoCarRentalUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // It combines models that related
        public class PaymentViewModel
        {
            public List<Car> Car { get; set; }
            public List<CarBrand> CarBrands { get; set; }
            public List<Payment> Payment { get; set; }
            public List<HajurKoCarRentalUser> Users { get; set; }
        }
        
        // Redirects user to the index view of the payment
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();
            var car = _context.Car!.ToList();
            var carBrands = _context.CarBrand!.ToList();
            var payment = _context.Payment!.ToList();
            var viewModel = new PaymentViewModel
            {
                Car = car,
                CarBrands = carBrands,
                Payment = payment,
                Users = users
            };

            return View(viewModel);
        }


    }
}
