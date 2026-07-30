using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using HajurKoCarRental.Data;
using HajurKoCarRental.Areas.Identity.Data;
using HajurKoCarRental.Services;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("HajurKoCarRentalContextConnection") ?? throw new InvalidOperationException("Connection string 'HajurKoCarRentalContextConnection' not found.");

builder.Services.AddDbContext<HajurKoCarRentalContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 25))));

builder.Services.AddDefaultIdentity<HajurKoCarRentalUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<HajurKoCarRentalContext>();

builder.Services.AddOptions<Microsoft.AspNetCore.Authentication.Google.GoogleOptions>(Microsoft.AspNetCore.Authentication.Google.GoogleDefaults.AuthenticationScheme)
    .Configure(googleOptions =>
    {
        googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        googleOptions.SignInScheme = IdentityConstants.ExternalScheme;
    });

builder.Services.AddAuthentication()
    .AddGoogle(Microsoft.AspNetCore.Authentication.Google.GoogleDefaults.AuthenticationScheme, _ => { });

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddScoped<FestivalService>();
builder.Services.AddScoped<RecommendationService>();
builder.Services.AddScoped<NotificationBadgeService>();
builder.Services.AddScoped<ChurnPredictionService>();
builder.Services.AddSingleton<PdfExportService>();
builder.Services.AddScoped<AnalyticsService>();
builder.Services.AddTransient<IEmailSender, IdentityEmailSender>();

var app = builder.Build();

// Apply database migrations automatically
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<HajurKoCarRentalContext>();
    context.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Lax
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();

app.UseAuthorization();

// Prevent browser caching so back button after logout never shows stale pages
app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();
app.MapHub<HajurKoCarRental.Hubs.ChatHub>("/chatHub");
app.Run();
