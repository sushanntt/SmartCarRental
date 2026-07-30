using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace HajurKoCarRental.Services
{
    public static class GmailSender
    {
        public static async Task SendAsync(
            string fromAddress,
            string appPassword,
            string toAddress,
            string subject,
            string body,
            string? replyTo = null,
            CancellationToken cancellationToken = default)
        {
            var password = appPassword.Replace(" ", "");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Smart Car Rental", fromAddress));
            message.To.Add(MailboxAddress.Parse(toAddress));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            if (!string.IsNullOrWhiteSpace(replyTo))
            {
                message.ReplyTo.Add(MailboxAddress.Parse(replyTo));
            }

            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls, cancellationToken);
            await client.AuthenticateAsync(fromAddress, password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}
