using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HajurKoCarRental.Data;
using HajurKoCarRental.Models;
using HajurKoCarRental.Services;
using Microsoft.AspNetCore.Identity;
using HajurKoCarRental.Areas.Identity.Data;

namespace HajurKoCarRental.Controllers
{
    // RentalHistoryController to handle rental history
    public class RentalHistoryController : Controller
    {
        private readonly HajurKoCarRentalContext _context;
        private readonly UserManager<HajurKoCarRentalUser> _userManager;
        private readonly PdfExportService _pdfService;

        public RentalHistoryController(HajurKoCarRentalContext context, UserManager<HajurKoCarRentalUser> userManager, PdfExportService pdfService)
        {
            _context = context;
            _userManager = userManager;
            _pdfService = pdfService;
        }

        // GET: RentalHistory
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            if (User.IsInRole("Customer"))
            {
                var unseenHistory = await _context.RentalHistory!.Where(r => r.UserId == currentUser.Id && !r.Seen).ToListAsync();
                if (unseenHistory.Any())
                {
                    foreach (var h in unseenHistory)
                    {
                        h.Seen = true;
                    }
                    await _context.SaveChangesAsync();
                }
                var hajurKoCarRentalContext = _context.RentalHistory!.Include(r => r.Car).Include(r => r.User).Include(r => r.PickupBranch).Where(r => r.UserId == currentUser.Id);
                return View(await hajurKoCarRentalContext.ToListAsync());
            }
                var allContext = _context.RentalHistory!.Include(r => r.Car).Include(r => r.User).Include(r => r.PickupBranch);
                return View(await allContext.ToListAsync());

        }

        public async Task<IActionResult> ExportExcel()
        {
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            List<RentalHistory> data;
            if (User.IsInRole("Customer"))
            {
                data = await _context.RentalHistory!.Include(r => r.Car).Include(r => r.User).Include(r => r.PickupBranch).Where(r => r.UserId == currentUser.Id).ToListAsync();
            }
            else
            {
                data = await _context.RentalHistory!.Include(r => r.Car).Include(r => r.User).Include(r => r.PickupBranch).ToListAsync();
            }

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("RentalHistory");
            ws.Cell(1, 1).Value = "User Email";
            ws.Cell(1, 2).Value = "Name";
            ws.Cell(1, 3).Value = "Address";
            ws.Cell(1, 4).Value = "Phone";
            ws.Cell(1, 5).Value = "Car Model";
            ws.Cell(1, 6).Value = "Car Number";
            ws.Cell(1, 7).Value = "Pickup Branch";
            ws.Cell(1, 8).Value = "Authorized By";
            ws.Cell(1, 9).Value = "Duration";
            ws.Cell(1, 10).Value = "Status";
            ws.Cell(1, 11).Value = "Amount";
            ws.Cell(1, 12).Value = "Payment Method";

            int row = 2;
            foreach (var item in data)
            {
                ws.Cell(row, 1).Value = item.User?.Email ?? "";
                ws.Cell(row, 2).Value = (item.User?.Firstname ?? "") + " " + (item.User?.Lastname ?? "");
                ws.Cell(row, 3).Value = item.User?.Address ?? "";
                ws.Cell(row, 4).Value = item.User?.PhoneNumber ?? "";
                ws.Cell(row, 5).Value = item.Car?.ModelName ?? "";
                ws.Cell(row, 6).Value = item.Car?.CarNumber ?? "";
                ws.Cell(row, 7).Value = item.PickupBranch?.Name ?? "-";
                ws.Cell(row, 8).Value = item.AuthorizedBy ?? "";
                ws.Cell(row, 9).Value = (item.Duration > 0 && item.Hours > 0) ? $"{item.Duration} days, {item.Hours} hrs" : (item.Duration > 0) ? $"{item.Duration} days" : $"{item.Hours} hrs";
                ws.Cell(row, 10).Value = item.Status ? "Approved" : "Rejected/Cancelled";
                ws.Cell(row, 11).Value = item.Amount;
                ws.Cell(row, 12).Value = item.PaymentMethod ?? "";
                row++;
            }

            ws.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "RentalHistory.xlsx");
        }

        public async Task<IActionResult> ExportPdf()
        {
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            List<RentalHistory> data;
            if (User.IsInRole("Customer"))
            {
                data = await _context.RentalHistory!.Include(r => r.Car).Include(r => r.User).Include(r => r.PickupBranch).Where(r => r.UserId == currentUser.Id).ToListAsync();
            }
            else
            {
                data = await _context.RentalHistory!.Include(r => r.Car).Include(r => r.User).Include(r => r.PickupBranch).ToListAsync();
            }

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><title>Rental History</title>");
            sb.AppendLine("<style>body{font-family:Arial,sans-serif;margin:20px;}h1{color:#333;}table{width:100%;border-collapse:collapse;margin-top:10px;font-size:11px;}th,td{border:1px solid #ddd;padding:3px 4px;text-align:left;word-wrap:break-word;}th{background-color:#4CAF50;color:white;}tr:nth-child(even){background-color:#f2f2f2;}</style></head><body>");
            sb.AppendLine("<h1>Rental History</h1>");
            sb.AppendLine("<table><thead><tr><th>User Email</th><th>Name</th><th>Address</th><th>Phone</th><th>Car Model</th><th>Car Number</th><th>Pickup Branch</th><th>Authorized By</th><th>Duration</th><th>Status</th><th>Amount</th><th>Payment Method</th></tr></thead><tbody>");

            foreach (var item in data)
            {
                var durationText = (item.Duration > 0 && item.Hours > 0) ? $"{item.Duration} days, {item.Hours} hrs" : (item.Duration > 0) ? $"{item.Duration} days" : $"{item.Hours} hrs";
                sb.AppendLine($"<tr><td>{System.Net.WebUtility.HtmlEncode(item.User?.Email ?? "")}</td><td>{System.Net.WebUtility.HtmlEncode((item.User?.Firstname ?? "") + " " + (item.User?.Lastname ?? ""))}</td><td>{System.Net.WebUtility.HtmlEncode(item.User?.Address ?? "")}</td><td>{System.Net.WebUtility.HtmlEncode(item.User?.PhoneNumber ?? "")}</td><td>{System.Net.WebUtility.HtmlEncode(item.Car?.ModelName ?? "")}</td><td>{System.Net.WebUtility.HtmlEncode(item.Car?.CarNumber ?? "")}</td><td>{System.Net.WebUtility.HtmlEncode(item.PickupBranch?.Name ?? "-")}</td><td>{System.Net.WebUtility.HtmlEncode(item.AuthorizedBy ?? "")}</td><td>{durationText}</td><td>{(item.Status ? "Approved" : "Rejected/Cancelled")}</td><td>{item.Amount}</td><td>{System.Net.WebUtility.HtmlEncode(item.PaymentMethod ?? "")}</td></tr>");
            }

            sb.AppendLine("</tbody></table></body></html>");
            var pdfBytes = await _pdfService.GeneratePdfAsync(sb.ToString());
            return File(pdfBytes, "application/pdf", "RentalHistory.pdf");
        }
    }
}
