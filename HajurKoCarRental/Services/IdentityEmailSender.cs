using Microsoft.AspNetCore.Identity.UI.Services;

namespace HajurKoCarRental.Services
{
    public class IdentityEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public IdentityEmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var from = _configuration["Email:FromAddress"];
            var password = _configuration["Email:Password"];

            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(password))
                return;

            await GmailSender.SendAsync(from, password, email, subject, htmlMessage);
        }
    }
}
