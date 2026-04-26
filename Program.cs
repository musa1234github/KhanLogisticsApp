using KhanLogistics.Bal;
using KhanLogistics.Dal;
using KhanLogistics.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddTransient<ISrvUser, UserServices>();
builder.Services.AddTransient<IRepUser, RepUser>();

builder.Services.AddDbContext<TransportMgmtContext>(options =>
{
    options.UseSqlServer(@"Server=MUSAKHAN\SQLEXPRESS;
    Database=KhanLogistics;Trusted_Connection=True;TrustServerCertificate=True;",
    sqlServerOptionsAction: sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(120);
    });
});

// EPPlus license setup
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

var app = builder.Build();

// Bind to all network interfaces (external access enabled)
app.Urls.Clear();
app.Urls.Add("http://0.0.0.0:5000");

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Users}/{action=Login}/{id?}");

app.Run();
