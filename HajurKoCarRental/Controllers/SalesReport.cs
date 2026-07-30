using System.Text;
using ClosedXML.Excel;
using HajurKoCarRental.Areas.Identity.Data;
using HajurKoCarRental.Data;
using HajurKoCarRental.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HajurKoCarRental.Services;


namespace HajurKoCarRental.Controllers
{
    // SalesReport class to manage sales report
    public class SalesReport : Controller
    {
        private readonly HajurKoCarRentalContext _context;
        private readonly SignInManager<HajurKoCarRentalUser> _signInManager;
        private readonly UserManager<HajurKoCarRentalUser> _userManager;
        private readonly IWebHostEnvironment _webHost;
        private readonly NotificationBadgeService _notificationBadgeService;
        private readonly PdfExportService _pdfService;

        public SalesReport(
            HajurKoCarRentalContext context,
            SignInManager<HajurKoCarRentalUser> signInManager,
            UserManager<HajurKoCarRentalUser> userManager,
            IWebHostEnvironment webHost,
            NotificationBadgeService notificationBadgeService,
            PdfExportService pdfService)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
            _webHost = webHost;
            _notificationBadgeService = notificationBadgeService;
            _pdfService = pdfService;
        }

        // Redirects user to the index of the sales report view where staff can view reports regarding sales
        public async Task<IActionResult> Index()
        {
            await _notificationBadgeService.MarkAdminSalesReportSeenAsync();
            var hajurKoCarRentalContext = _context.RentalHistory!.Include(c => c.Car).Include(c => c.User).Where(w => w.Status == true);
            return View(await hajurKoCarRentalContext!.ToListAsync());
        }

        // Sorts sales report data in the index
        public async Task<IActionResult> FilterData(string searchText)
        {
            await _notificationBadgeService.MarkAdminSalesReportSeenAsync();
            var hajurKoCarRentalContext = _context.RentalHistory!.Include(c => c.Car).Include(c => c.User).Where(w => w.Status == true);
            List<RentalHistory> myList = new List<RentalHistory>();

            if (searchText == "Ascending")
            {
                myList = hajurKoCarRentalContext.OrderBy(item => item.RentedDate).ToList();
            } else {
                myList = hajurKoCarRentalContext.OrderByDescending(item => item.RentedDate).ToList();
            }

            return View("Index", myList.ToList());
        }

        // Filters sales report data according to the selected date range
        public async Task<IActionResult> FilterDate(DateTime? startDate, DateTime? endDate)
        {
            await _notificationBadgeService.MarkAdminSalesReportSeenAsync();
            var query = _context.RentalHistory!
                .Include(c => c.Car)
                .Include(c => c.User)
                .Where(w => w.Status == true)
                .AsQueryable();

            if (startDate.HasValue && endDate.HasValue)
            {
                query = query.Where(s => s.RentedDate >= startDate.Value && s.RentedDate <= endDate.Value);
                var sales = await query.ToListAsync();
                return View("Index", sales);
            }
            // If dates are not provided, redirect to the main Index view
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ExportExcel()
        {
            var data = await _context.RentalHistory!.Include(c => c.Car).Include(c => c.User).Where(w => w.Status == true).ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("SalesReport");
            ws.Cell(1, 1).Value = "User Email";
            ws.Cell(1, 2).Value = "Name";
            ws.Cell(1, 3).Value = "Rented Date";
            ws.Cell(1, 4).Value = "Car Model";
            ws.Cell(1, 5).Value = "Car Number";
            ws.Cell(1, 6).Value = "Amount";
            ws.Cell(1, 7).Value = "Authorized By";
            ws.Cell(1, 8).Value = "Status";

            int row = 2;
            foreach (var item in data)
            {
                ws.Cell(row, 1).Value = item.User?.Email ?? "";
                ws.Cell(row, 2).Value = (item.User?.Firstname ?? "") + " " + (item.User?.Lastname ?? "");
                ws.Cell(row, 3).Value = item.RentedDate?.ToString("yyyy-MM-dd") ?? "";
                ws.Cell(row, 4).Value = item.Car?.ModelName ?? "";
                ws.Cell(row, 5).Value = item.Car?.CarNumber ?? "";
                ws.Cell(row, 6).Value = item.Amount;
                ws.Cell(row, 7).Value = item.AuthorizedBy ?? "";
                ws.Cell(row, 8).Value = item.Status ? "Approved" : "Rejected/Cancelled";
                row++;
            }

            ws.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SalesReport.xlsx");
        }

        public async Task<IActionResult> ExportPdf()
        {
            var data = await _context.RentalHistory!.Include(c => c.Car).Include(c => c.User).Where(w => w.Status == true).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><title>Sales Report</title>");
            sb.AppendLine("<style>body{font-family:Arial,sans-serif;margin:20px;}h1{color:#333;}table{width:100%;border-collapse:collapse;margin-top:10px;font-size:11px;}th,td{border:1px solid #ddd;padding:3px 4px;text-align:left;word-wrap:break-word;}th{background-color:#2196F3;color:white;}tr:nth-child(even){background-color:#f2f2f2;}</style></head><body>");
            sb.AppendLine("<h1>Sales Report</h1>");
            sb.AppendLine("<table><thead><tr><th>User Email</th><th>Name</th><th>Rented Date</th><th>Car Model</th><th>Car Number</th><th>Amount</th><th>Authorized By</th><th>Status</th></tr></thead><tbody>");

            foreach (var item in data)
            {
                sb.AppendLine($"<tr><td>{System.Net.WebUtility.HtmlEncode(item.User?.Email ?? "")}</td><td>{System.Net.WebUtility.HtmlEncode((item.User?.Firstname ?? "") + " " + (item.User?.Lastname ?? ""))}</td><td>{item.RentedDate?.ToString("yyyy-MM-dd") ?? ""}</td><td>{System.Net.WebUtility.HtmlEncode(item.Car?.ModelName ?? "")}</td><td>{System.Net.WebUtility.HtmlEncode(item.Car?.CarNumber ?? "")}</td><td>{item.Amount}</td><td>{System.Net.WebUtility.HtmlEncode(item.AuthorizedBy ?? "")}</td><td>{(item.Status ? "Approved" : "Rejected/Cancelled")}</td></tr>");
            }

            sb.AppendLine("</tbody></table></body></html>");
            var pdfBytes = await _pdfService.GeneratePdfAsync(sb.ToString());
            return File(pdfBytes, "application/pdf", "SalesReport.pdf");
        }
    }
}
