using System.Text;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HajurKoCarRental.Services;
using HajurKoCarRental.Data;
using HajurKoCarRental.Models;

namespace HajurKoCarRental.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ChurnReportController : Controller
    {
        private readonly ChurnPredictionService _churnService;
        private readonly IConfiguration _configuration;
        private readonly HajurKoCarRentalContext _context;
        private readonly PdfExportService _pdfService;

        public ChurnReportController(ChurnPredictionService churnService, IConfiguration configuration, HajurKoCarRentalContext context, PdfExportService pdfService)
        {
            _churnService = churnService;
            _configuration = configuration;
            _context = context;
            _pdfService = pdfService;
        }

        public async Task<IActionResult> Index()
        {
            var predictions = await _churnService.GetChurnPredictionsAsync();

            ViewBag.HighCount = predictions.Count(p => p.RiskLevel == "High");
            ViewBag.MediumCount = predictions.Count(p => p.RiskLevel == "Medium");
            ViewBag.LowCount = predictions.Count(p => p.RiskLevel == "Low");
            ViewBag.TotalCount = predictions.Count;

            return View(predictions);
        }

        [HttpPost]
        public async Task<IActionResult> SendRetentionOffer(string userId, string offerType, int? customPercentage, string customMessage, string customerName, string customerEmail)
        {
            if (string.IsNullOrWhiteSpace(customerEmail))
            {
                TempData["ErrorMessage"] = "Customer email is missing.";
                return RedirectToAction(nameof(Index));
            }

            var discountCode = GenerateDiscountCode();
            string subject;
            string body;

            int discountPercent;
            switch (offerType)
            {
                case "Discount10":
                    discountPercent = 10;
                    subject = "Special Offer: 10% Off Your Next Rental!";
                    body = $"Dear {customerName},\n\nWe miss you at Smart Car Rental! As a valued customer, here's a special 10% discount on your next rental.\n\nYour code: {discountCode}\n\nApply this code on the rental form to get 10% off.\n\nWe hope to see you soon!\n\nSmart Car Rental Team";
                    break;
                case "Discount20":
                    discountPercent = 20;
                    subject = "Exclusive Offer: 20% Off – Come Back and Drive!";
                    body = $"Dear {customerName},\n\nWe noticed it's been a while. Here's an exclusive 20% discount to welcome you back.\n\nYour code: {discountCode}\n\nApply this code on the rental form to get 20% off.\n\nSafe travels!\nSmart Car Rental Team";
                    break;
                case "FreeUpgrade":
                    discountPercent = 15;
                    subject = "Complimentary Upgrade on Your Next Rental!";
                    body = $"Dear {customerName},\n\nAs a valued customer, enjoy a 15% discount on your next rental.\n\nYour code: {discountCode}\n\nApply this code on the rental form.\n\nWe look forward to serving you again!\n\nSmart Car Rental Team";
                    break;
                default:
                    subject = "We Miss You – Special Offer Inside!";
                    if (customPercentage == null || customPercentage < 10 || customPercentage > 100)
                    {
                        TempData["ErrorMessage"] = "Custom discount must be between 10 and 100.";
                        return RedirectToAction(nameof(Index));
                    }
                    discountPercent = customPercentage.Value;
                    body = $"Dear {customerName},\n\nWe'd love to have you back at Smart Car Rental. Here's a {discountPercent}% discount just for you.\n\nYour code: {discountCode}\n\n{customMessage}\n\nBest regards,\nSmart Car Rental Team";
                    break;
            }

            try
            {
                var from = _configuration["Email:FromAddress"];
                var password = _configuration["Email:Password"];

                await GmailSender.SendAsync(from, password, customerEmail, subject, body);

                var offer = new RetentionOffer
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    OfferType = offerType,
                    DiscountCode = discountCode,
                    SentAt = DateTime.Now,
                    Email = customerEmail,
                    CustomMessage = offerType == "Custom" ? customMessage : null,
                    DiscountPercent = discountPercent
                };
                _context.RetentionOffer!.Add(offer);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Retention offer sent to {customerName} ({customerEmail}). Code: {discountCode}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to send offer: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ExportExcel()
        {
            var predictions = await _churnService.GetChurnPredictionsAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("ChurnReport");
            ws.Cell(1, 1).Value = "Customer Name";
            ws.Cell(1, 2).Value = "Email";
            ws.Cell(1, 3).Value = "Risk Level";
            ws.Cell(1, 4).Value = "Churn Score";
            ws.Cell(1, 5).Value = "Days Since Last Rental";
            ws.Cell(1, 6).Value = "Rentals (12mo)";
            ws.Cell(1, 7).Value = "Total Spent";
            ws.Cell(1, 8).Value = "Recency Score";
            ws.Cell(1, 9).Value = "Frequency Score";
            ws.Cell(1, 10).Value = "Monetary Score";
            ws.Cell(1, 11).Value = "Flags";

            int row = 2;
            foreach (var item in predictions)
            {
                var flags = new List<string>();
                if (item.HasUnpaidDamage) flags.Add("Damage");
                if (!item.IsProfileComplete) flags.Add("Profile");
                if (item.IsRegularCustomer) flags.Add("Regular");

                ws.Cell(row, 1).Value = item.CustomerName;
                ws.Cell(row, 2).Value = item.Email;
                ws.Cell(row, 3).Value = item.RiskLevel;
                ws.Cell(row, 4).Value = item.ChurnRiskScore;
                ws.Cell(row, 5).Value = item.DaysSinceLastRental;
                ws.Cell(row, 6).Value = item.RentalCount;
                ws.Cell(row, 7).Value = item.TotalSpent;
                ws.Cell(row, 8).Value = item.RecencyScore;
                ws.Cell(row, 9).Value = item.FrequencyScore;
                ws.Cell(row, 10).Value = item.MonetaryScore;
                ws.Cell(row, 11).Value = flags.Count > 0 ? string.Join(", ", flags) : "None";
                row++;
            }

            ws.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ChurnReport.xlsx");
        }

        public async Task<IActionResult> ExportPdf()
        {
            var predictions = await _churnService.GetChurnPredictionsAsync();

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><title>Churn Prediction Report</title>");
            sb.AppendLine("<style>body{font-family:Arial,sans-serif;margin:20px;}h1{color:#333;}table{width:100%;border-collapse:collapse;margin-top:10px;}th,td{border:1px solid #ddd;padding:8px;text-align:left;}th{background-color:#6f42c1;color:white;}tr:nth-child(even){background-color:#f2f2f2;}.high{color:#dc3545;font-weight:bold;}.medium{color:#b86b1d;font-weight:bold;}.low{color:#198754;font-weight:bold;}</style></head><body>");
            sb.AppendLine("<h1>Customer Churn Prediction Report</h1>");
            sb.AppendLine("<table><thead><tr><th>Customer</th><th>Email</th><th>Risk Level</th><th>Churn Score</th><th>Days Since Last</th><th>Rentals (12mo)</th><th>Total Spent</th><th>Recency</th><th>Frequency</th><th>Monetary</th><th>Flags</th></tr></thead><tbody>");

            foreach (var item in predictions)
            {
                var riskClass = item.RiskLevel.ToLower();
                var scorePct = item.ChurnRiskScore.ToString("P1");
                var flags = new List<string>();
                if (item.HasUnpaidDamage) flags.Add("Damage");
                if (!item.IsProfileComplete) flags.Add("Profile");
                if (item.IsRegularCustomer) flags.Add("Regular");
                var flagsStr = flags.Count > 0 ? string.Join(", ", flags) : "None";

                sb.AppendLine($"<tr><td>{System.Net.WebUtility.HtmlEncode(item.CustomerName)}</td><td>{System.Net.WebUtility.HtmlEncode(item.Email)}</td><td class='{riskClass}'>{System.Net.WebUtility.HtmlEncode(item.RiskLevel)}</td><td>{scorePct}</td><td>{item.DaysSinceLastRental}</td><td>{item.RentalCount}</td><td>{item.TotalSpent:N0}</td><td>{item.RecencyScore:F2}</td><td>{item.FrequencyScore:F2}</td><td>{item.MonetaryScore:F2}</td><td>{System.Net.WebUtility.HtmlEncode(flagsStr)}</td></tr>");
            }

            sb.AppendLine("</tbody></table></body></html>");
            var pdfBytes = await _pdfService.GeneratePdfAsync(sb.ToString());
            return File(pdfBytes, "application/pdf", "ChurnReport.pdf");
        }

        private static string GenerateDiscountCode()
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var code = new char[8];
            var rng = new Random();
            code[0] = 'R'; code[1] = 'E'; code[2] = 'T';
            for (int i = 3; i < 8; i++)
            {
                code[i] = chars[rng.Next(chars.Length)];
            }
            return new string(code);
        }
    }
}
