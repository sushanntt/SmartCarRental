using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using HajurKoCarRental.Areas.Identity.Data;
using HajurKoCarRental.Data;
using HajurKoCarRental.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace HajurKoCarRental.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<HajurKoCarRentalUser> _signInManager;
        private readonly UserManager<HajurKoCarRentalUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ExternalLoginModel> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly HajurKoCarRentalContext _context;

        public ExternalLoginModel(
            SignInManager<HajurKoCarRentalUser> signInManager,
            UserManager<HajurKoCarRentalUser> userManager,
            ILogger<ExternalLoginModel> logger,
            IEmailSender emailSender,
            RoleManager<IdentityRole> roleManager,
            HajurKoCarRentalContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _emailSender = emailSender;
            _roleManager = roleManager;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ProviderDisplayName { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [Display(Name = "First Name")]
            public string Firstname { get; set; }

            [Required]
            [Display(Name = "Last Name")]
            public string Lastname { get; set; }

            [Required]
            [Display(Name = "Address")]
            public string Address { get; set; }

            [Required]
            [Display(Name = "Phone Number")]
            [StringLength(10, MinimumLength = 10, ErrorMessage = "Phone number must be exactly 10 digits.")]
            [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be exactly 10 digits.")]
            public string PhoneNumber { get; set; }
        }

        public IActionResult OnGet() => RedirectToPage("./Login");

        public IActionResult OnPost(string provider, string returnUrl = null, string action = null)
        {
            var redirectUrl = $"{Request.Scheme}://{Request.Host}/Identity/Account/ExternalLogin?handler=Callback&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}&action={action}";
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null, string action = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity.Name, info.LoginProvider);
                var user = await _userManager.FindByEmailAsync(info.Principal.FindFirstValue(ClaimTypes.Email));
                if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return Redirect("/Analytics");
                }
                return LocalRedirect(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }
            else
            {
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                if (!string.IsNullOrEmpty(email))
                {
                    var existingUser = await _userManager.FindByEmailAsync(email);
                    if (existingUser != null && action == "register")
                    {
                        ErrorMessage = $"An account with email {email} already exists. Please log in with your existing credentials.";
                        return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
                    }
                }
                ReturnUrl = returnUrl;
                ProviderDisplayName = info.ProviderDisplayName;
                if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
                {
                    Input = new InputModel
                    {
                        Email = info.Principal.FindFirstValue(ClaimTypes.Email),
                        Firstname = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "",
                        Lastname = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? ""
                    };
                }
                return Page();
            }
        }

        public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information during confirmation.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(Input.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError(string.Empty, "Email is already registered.");
                    ProviderDisplayName = info.ProviderDisplayName;
                    ReturnUrl = returnUrl;
                    return Page();
                }

                var token = Guid.NewGuid().ToString("N");
                var pending = new PendingRegistration
                {
                    Token = token,
                    Firstname = Input.Firstname,
                    Lastname = Input.Lastname,
                    Address = Input.Address ?? "",
                    Email = Input.Email,
                    PasswordHash = null,
                    PhoneNumber = Input.PhoneNumber ?? "",
                    LoginProvider = info.LoginProvider,
                    ProviderKey = info.ProviderKey,
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

                _logger.LogInformation("Pending external registration created for {Email}", Input.Email);

                return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });
            }

            ProviderDisplayName = info.ProviderDisplayName;
            ReturnUrl = returnUrl;
            return Page();
        }
    }
}
