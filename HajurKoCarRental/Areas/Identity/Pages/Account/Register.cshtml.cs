// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using HajurKoCarRental.Areas.Identity.Data;
using HajurKoCarRental.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using HajurKoCarRental.Data;

namespace HajurKoCarRental.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<HajurKoCarRentalUser> _signInManager;
        private readonly UserManager<HajurKoCarRentalUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly HajurKoCarRentalContext _context;

        public RegisterModel(
            UserManager<HajurKoCarRentalUser> userManager,
            SignInManager<HajurKoCarRentalUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            RoleManager<IdentityRole> roleManager,
            HajurKoCarRentalContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _roleManager = roleManager;
            _context = context;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            [Required]
            [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "First name must contain only letters.")]
            [Display(Name = "First Name")]
            public string Firstname { get; set; }

            [Required]
            [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Last name must contain only letters.")]
            [Display(Name = "Last Name")]
            public string Lastname { get; set; }

            [Required]
            [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Address must contain only letters.")]
            [Display(Name = "Address")]
            public string Address { get; set; }

            [Required]
            [StringLength(10, MinimumLength = 10, ErrorMessage = "Phone number must be exactly 10 digits.")]
            [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be exactly 10 digits.")]
            [Display(Name = "Phone Number")]
            public string PhoneNumber { get; set; }


            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            [RegularExpression(@"^[a-zA-Z].*", ErrorMessage = "Email must start with a letter.")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [RegularExpression(@"^(?!\d)(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Password must not start with a number, and must contain at least one uppercase letter and one number.")]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }


        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(Input.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError(string.Empty, "Email is already registered.");
                    return Page();
                }

                var token = Guid.NewGuid().ToString("N");
                var hasher = new PasswordHasher<HajurKoCarRentalUser>();
                var pending = new PendingRegistration
                {
                    Token = token,
                    Firstname = Input.Firstname,
                    Lastname = Input.Lastname,
                    Address = Input.Address,
                    Email = Input.Email,
                    PasswordHash = hasher.HashPassword(null, Input.Password),
                    PhoneNumber = Input.PhoneNumber,
                    CreatedAt = DateTime.UtcNow
                };
                _context.PendingRegistration.Add(pending);
                await _context.SaveChangesAsync();

                var confirmationLink = Url.Page(
                    "/Account/ConfirmRegistration",
                    pageHandler: null,
                    values: new { area = "Identity", token },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(Input.Email, "Confirm your registration",
                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(confirmationLink)}'>clicking here</a>.");

                _logger.LogInformation("Pending registration created for {Email}", Input.Email);

                return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });
            }

            return Page();
        }
    }
}
