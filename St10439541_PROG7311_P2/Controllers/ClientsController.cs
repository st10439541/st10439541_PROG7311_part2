using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using St10439541_PROG7311_P2.Data;
using St10439541_PROG7311_P2.Models;
using St10439541_PROG7311_P2.Services;
using St10439541_PROG7311_P2.Services.Observers;

namespace St10439541_PROG7311_P2.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ClientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClientsController> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ObserverManager _observerManager;
        private static Dictionary<int, string> _userPasswords = new Dictionary<int, string>();

        public ClientsController(
            ApplicationDbContext context,
            ILogger<ClientsController> logger,
            IServiceProvider serviceProvider,
            ObserverManager observerManager)
        {
            _context = context;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _observerManager = observerManager;
        }

        // GET: Clients
        public async Task<IActionResult> Index(string? roleFilter)
        {
            System.Diagnostics.Debug.WriteLine(">>> ClientsController.Index() was called <<<");

            var clients = await _context.Clients
                .Include(c => c.Contracts)
                .ToListAsync();

            using var scope = _serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var allUsers = await userManager.Users.ToListAsync();

            if (!string.IsNullOrEmpty(roleFilter))
            {
                var filteredUserIds = allUsers
                    .Where(u => roleFilter == "Admin" ? u.IsAdmin : !u.IsAdmin)
                    .Select(u => u.ClientId)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .ToList();

                clients = clients.Where(c => filteredUserIds.Contains(c.ClientId)).ToList();
            }

            ViewBag.Users = allUsers;
            ViewBag.RoleFilter = roleFilter;
            ViewBag.UserPasswords = _userPasswords;

            return View(clients);
        }

        // GET: Clients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients
                .Include(c => c.Contracts)
                .FirstOrDefaultAsync(m => m.ClientId == id);

            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

        // GET: Clients/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Clients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,ContactEmail,ContactPhone,Address,Region,SelectedRole,Password,ConfirmPassword")] Client client)
        {
            if (ModelState.IsValid)
            {
                if (client.Password != client.ConfirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword", "Passwords do not match.");
                    return View(client);
                }

                if (!string.IsNullOrEmpty(client.Password) && client.Password.Length < 6)
                {
                    ModelState.AddModelError("Password", "Password must be at least 6 characters.");
                    return View(client);
                }

                using var scope = _serviceProvider.CreateScope();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                var existingUser = await userManager.FindByEmailAsync(client.ContactEmail);
                if (existingUser != null)
                {
                    ModelState.AddModelError("ContactEmail", "This email is already registered.");
                    return View(client);
                }

                _context.Add(client);
                await _context.SaveChangesAsync();

                await _observerManager.OnClientCreated(client);

                var user = new User
                {
                    UserName = client.ContactEmail,
                    Email = client.ContactEmail,
                    FullName = client.Name,
                    CompanyName = client.Name,
                    Address = client.Address,
                    Region = client.Region,
                    IsAdmin = client.SelectedRole == "Admin",
                    RegistrationDate = DateTime.Now,
                    ClientId = client.ClientId,
                    PlainTextPassword = client.Password
                };

                var result = await userManager.CreateAsync(user, client.Password);

                if (result.Succeeded)
                {
                    _userPasswords[client.ClientId] = client.Password;

                    if (client.SelectedRole == "Admin")
                    {
                        await userManager.AddToRoleAsync(user, "Admin");
                    }
                    else
                    {
                        await userManager.AddToRoleAsync(user, "User");
                    }

                    _logger.LogInformation("Created new client: {ClientName} (ID: {ClientId}) with role: {Role}", client.Name, client.ClientId, client.SelectedRole);
                    TempData["SuccessMessage"] = $"Client '{client.Name}' created successfully with role: {client.SelectedRole}! Password: {client.Password}";

                    // FIXED: Use RedirectToAction, NOT return View("Index", client)
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _context.Clients.Remove(client);
                    await _context.SaveChangesAsync();

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(client);
                }
            }

            return View(client);
        }

        // GET: Clients/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients.FindAsync(id);
            if (client == null)
            {
                return NotFound();
            }

            using var scope = _serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await userManager.Users.FirstOrDefaultAsync(u => u.ClientId == client.ClientId);

            if (user != null)
            {
                client.SelectedRole = user.IsAdmin ? "Admin" : "User";
            }
            else
            {
                client.SelectedRole = "User";
            }

            return View(client);
        }

        // POST: Clients/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ClientId,Name,ContactEmail,ContactPhone,Address,Region,SelectedRole,NewPassword,ConfirmNewPassword")] Client client)
        {
            if (id != client.ClientId)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(client.NewPassword))
            {
                if (client.NewPassword != client.ConfirmNewPassword)
                {
                    ModelState.AddModelError("ConfirmNewPassword", "Passwords do not match.");
                    return View(client);
                }

                if (client.NewPassword.Length < 6)
                {
                    ModelState.AddModelError("NewPassword", "Password must be at least 6 characters.");
                    return View(client);
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(client);
                    await _context.SaveChangesAsync();

                    await _observerManager.OnClientUpdated(client);

                    using var scope = _serviceProvider.CreateScope();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                    var user = await userManager.Users.FirstOrDefaultAsync(u => u.ClientId == client.ClientId);

                    if (user != null)
                    {
                        user.FullName = client.Name;
                        user.CompanyName = client.Name;
                        user.Address = client.Address;
                        user.Region = client.Region;
                        user.Email = client.ContactEmail;
                        user.UserName = client.ContactEmail;

                        if (!string.IsNullOrEmpty(client.NewPassword))
                        {
                            var token = await userManager.GeneratePasswordResetTokenAsync(user);
                            var passwordResult = await userManager.ResetPasswordAsync(user, token, client.NewPassword);

                            if (!passwordResult.Succeeded)
                            {
                                foreach (var error in passwordResult.Errors)
                                {
                                    ModelState.AddModelError(string.Empty, $"Password error: {error.Description}");
                                }
                                return View(client);
                            }

                            _userPasswords[client.ClientId] = client.NewPassword;
                            _logger.LogInformation("Password changed for user: {UserEmail}", user.Email);
                        }

                        bool newIsAdmin = client.SelectedRole == "Admin";
                        bool oldIsAdmin = user.IsAdmin;
                        user.IsAdmin = newIsAdmin;

                        await userManager.UpdateAsync(user);

                        if (newIsAdmin && !oldIsAdmin)
                        {
                            if (await userManager.IsInRoleAsync(user, "User"))
                            {
                                await userManager.RemoveFromRoleAsync(user, "User");
                            }
                            if (!await userManager.IsInRoleAsync(user, "Admin"))
                            {
                                await userManager.AddToRoleAsync(user, "Admin");
                            }
                        }
                        else if (!newIsAdmin && oldIsAdmin)
                        {
                            if (await userManager.IsInRoleAsync(user, "Admin"))
                            {
                                await userManager.RemoveFromRoleAsync(user, "Admin");
                            }
                            if (!await userManager.IsInRoleAsync(user, "User"))
                            {
                                await userManager.AddToRoleAsync(user, "User");
                            }
                        }

                        if (!string.IsNullOrEmpty(client.NewPassword))
                        {
                            TempData["SuccessMessage"] = $"Client '{client.Name}' updated successfully! Password has been changed. Role: {client.SelectedRole}";
                        }
                        else
                        {
                            TempData["SuccessMessage"] = $"Client '{client.Name}' updated successfully! Role: {client.SelectedRole}";
                        }
                    }
                    else
                    {
                        string password = !string.IsNullOrEmpty(client.NewPassword) ? client.NewPassword : "123456";
                        var newUser = new User
                        {
                            UserName = client.ContactEmail,
                            Email = client.ContactEmail,
                            FullName = client.Name,
                            CompanyName = client.Name,
                            Address = client.Address,
                            Region = client.Region,
                            IsAdmin = client.SelectedRole == "Admin",
                            RegistrationDate = DateTime.Now,
                            ClientId = client.ClientId
                        };

                        var result = await userManager.CreateAsync(newUser, password);
                        if (result.Succeeded)
                        {
                            _userPasswords[client.ClientId] = password;
                            if (client.SelectedRole == "Admin")
                            {
                                await userManager.AddToRoleAsync(newUser, "Admin");
                            }
                            else
                            {
                                await userManager.AddToRoleAsync(newUser, "User");
                            }
                            TempData["SuccessMessage"] = $"Client '{client.Name}' updated successfully! New user account created.";
                        }
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClientExists(client.ClientId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                // FIXED: Use RedirectToAction, NOT return View("Index", client)
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        // GET: Clients/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients
                .Include(c => c.Contracts)
                .FirstOrDefaultAsync(m => m.ClientId == id);

            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

        // POST: Clients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client != null)
            {
                using var scope = _serviceProvider.CreateScope();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                var user = await userManager.Users.FirstOrDefaultAsync(u => u.ClientId == client.ClientId);

                if (user != null)
                {
                    await userManager.DeleteAsync(user);
                }

                _userPasswords.Remove(client.ClientId);

                await _observerManager.OnClientDeleted(client);

                _context.Clients.Remove(client);
                _logger.LogWarning("Deleted client: {ClientName} (ID: {ClientId})", client.Name, client.ClientId);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Client deleted successfully!";

            // FIXED: Use RedirectToAction
            return RedirectToAction(nameof(Index));
        }

        private bool ClientExists(int id)
        {
            return _context.Clients.Any(e => e.ClientId == id);
        }
    }
}