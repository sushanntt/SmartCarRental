using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HajurKoCarRental.Areas.Identity.Data;
using HajurKoCarRental.Data;
using HajurKoCarRental.Models;
using Microsoft.Extensions.Logging;

namespace HajurKoCarRental.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ConfirmRegistrationModel : PageModel
    {
        private readonly UserManager<HajurKoCarRentalUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly HajurKoCarRentalContext _context;
        private readonly ILogger<ConfirmRegistrationModel> _logger;
        private readonly SignInManager<HajurKoCarRentalUser> _signInManager;

        public ConfirmRegistrationModel(
            UserManager<HajurKoCarRentalUser> userManager,
            RoleManager<IdentityRole> roleManager,
            HajurKoCarRentalContext context,
            ILogger<ConfirmRegistrationModel> logger,
            SignInManager<HajurKoCarRentalUser> signInManager = null)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> OnGetAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToPage("/Index");

            var pending = _context.PendingRegistration.FirstOrDefault(p => p.Token == token);
            if (pending == null)
                return RedirectToPage("/Index");

            var existingUser = await _userManager.FindByEmailAsync(pending.Email);
            if (existingUser != null)
            {
                _context.PendingRegistration.Remove(pending);
                await _context.SaveChangesAsync();
                ViewData["Error"] = "This email is already registered. Please sign in.";
                return Page();
            }

            var user = new HajurKoCarRentalUser
            {
                UserName = pending.Email,
                Email = pending.Email,
                Firstname = pending.Firstname,
                Lastname = pending.Lastname,
                Address = pending.Address,
                PhoneNumber = pending.PhoneNumber,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to create user for {Email}: {Errors}", pending.Email,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                ViewData["Error"] = "Registration failed. Please try again.";
                return Page();
            }

            if (!string.IsNullOrEmpty(pending.PasswordHash))
            {
                user.PasswordHash = pending.PasswordHash;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);
                    _logger.LogError("Failed to set password hash for {Email}", pending.Email);
                    ViewData["Error"] = "Registration failed. Please try again.";
                    return Page();
                }
            }

            if (!string.IsNullOrEmpty(pending.LoginProvider) && !string.IsNullOrEmpty(pending.ProviderKey))
            {
                var loginResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(
                    pending.LoginProvider, pending.ProviderKey, pending.LoginProvider));
                if (!loginResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);
                    _logger.LogError("Failed to link external login for {Email}", pending.Email);
                    ViewData["Error"] = "Registration failed. Please try again.";
                    return Page();
                }
            }

            var userCount = _userManager.Users.Count();
            var role = userCount == 1 ? "Admin" : "Customer";
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));
            await _userManager.AddToRoleAsync(user, role);

            _context.PendingRegistration.Remove(pending);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User confirmed registration: {Email}", pending.Email);
            return Page();
        }
    }
}
