using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using St10439541_PROG7311_P2.Data;
using St10439541_PROG7311_P2.Models;
using St10439541_PROG7311_P2.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add DbContext with SQLite (no server required!)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity with User
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

// Register custom services
builder.Services.AddScoped<IFileValidationService, FileValidationService>();
builder.Services.AddScoped<ICurrencyExchangeService, CurrencyExchangeService>();
builder.Services.AddScoped<IUserAuthorizationService, UserAuthorizationService>();
builder.Services.AddHttpContextAccessor();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Create database and roles on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // This will create the database file if it doesn't exist
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();

        // Then create roles and admin user
        await CreateRolesAndAdminUserAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while creating the database or admin user.");
    }
}

// Ensure uploads directory exists
string uploadsPath = Path.Combine(app.Environment.WebRootPath, "uploads", "contracts");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.Run();

// Helper method to create roles and admin user
async Task CreateRolesAndAdminUserAsync(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
    var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

    // Create roles
    string[] roles = { "Admin", "User" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
            Console.WriteLine($"Created role: {role}");
        }
    }

    // admin account
    var adminEmail = "admin@techmove.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        var admin = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "System Administrator",
            CompanyName = "TechMove Logistics",
            IsAdmin = true,
            RegistrationDate = DateTime.Now
        };

        var result = await userManager.CreateAsync(admin, "Admin@123");

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
            Console.WriteLine("Created admin user");

            // Create a client record for admin
            var client = new Client
            {
                Name = "TechMove Logistics",
                ContactEmail = adminEmail,
                ContactPhone = "000-000-0000",
                Address = "Head Office",
                Region = "Global"
            };

            context.Clients.Add(client);
            await context.SaveChangesAsync();

            admin.ClientId = client.ClientId;
            await userManager.UpdateAsync(admin);

            Console.WriteLine("Created client record for admin");
        }
        else
        {
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"Error creating admin: {error.Description}");
            }
        }
    }
}