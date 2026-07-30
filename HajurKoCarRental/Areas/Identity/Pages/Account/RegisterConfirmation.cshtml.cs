using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HajurKoCarRental.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        public string Email { get; set; }
        public string ReturnUrl { get; set; }

        public IActionResult OnGet(string email, string returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToPage("/Index");

            Email = email;
            ReturnUrl = returnUrl;
            return Page();
        }
    }
}
