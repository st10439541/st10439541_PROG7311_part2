using Microsoft.EntityFrameworkCore;
using St10439541_PROG7311_P2.Data;
using St10439541_PROG7311_P2.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Use SQLite instead of SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=TechMoveLogistics.db"));

// Register custom services
builder.Services.AddScoped<IFileValidationService, FileValidationService>();
builder.Services.AddScoped<ICurrencyExchangeService, CurrencyExchangeService>();

// Register HttpClient for API calls
builder.Services.AddHttpClient<CurrencyExchangeService>();

// Add caching for exchange rates
builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}

// Ensure uploads directory exists
string uploadsPath = Path.Combine(app.Environment.WebRootPath, "uploads", "contracts");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.Run();