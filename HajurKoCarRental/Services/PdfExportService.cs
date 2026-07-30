using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace HajurKoCarRental.Services
{
    public class PdfExportService
    {
        private IBrowser? _browser;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private async Task<IBrowser> GetBrowserAsync()
        {
            if (_browser is { IsClosed: false })
                return _browser;

            await _semaphore.WaitAsync();
            try
            {
                if (_browser is { IsClosed: false })
                    return _browser;

                await new BrowserFetcher().DownloadAsync();
                _browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
                });
                return _browser;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<byte[]> GeneratePdfAsync(string htmlContent)
        {
            var browser = await GetBrowserAsync();
            await using var page = await browser.NewPageAsync();
            await page.SetContentAsync(htmlContent);
            return await page.PdfDataAsync(new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true
            });
        }
    }
}
