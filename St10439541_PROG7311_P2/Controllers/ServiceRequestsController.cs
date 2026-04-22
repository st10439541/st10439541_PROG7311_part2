using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using St10439541_PROG7311_P2.Data;
using St10439541_PROG7311_P2.Models;
using St10439541_PROG7311_P2.Services;

namespace St10439541_PROG7311_P2.Controllers
{
    public class ServiceRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrencyExchangeService _currencyService;
        private readonly ILogger<ServiceRequestsController> _logger;

        public ServiceRequestsController(
            ApplicationDbContext context,
            ICurrencyExchangeService currencyService,
            ILogger<ServiceRequestsController> logger)
        {
            _context = context;
            _currencyService = currencyService;
            _logger = logger;
        }

        // GET: ServiceRequests
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var serviceRequests = await _context.ServiceRequests
                .Include(s => s.Contract)
                .ThenInclude(c => c!.Client)
                .ToListAsync();
            return View(serviceRequests);
        }

        // GET: ServiceRequests/Details/5
        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceRequest = await _context.ServiceRequests
                .Include(s => s.Contract)
                .ThenInclude(c => c!.Client)
                .FirstOrDefaultAsync(m => m.ServiceRequestId == id);

            if (serviceRequest == null)
            {
                return NotFound();
            }

            return View(serviceRequest);
        }

        // GET: ServiceRequests/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            var activeContracts = await _context.Contracts
                .Include(c => c.Client)
                .Where(c => c.Status == ContractStatus.Active && c.EndDate >= DateTime.Today && c.IsSignedByClient == true)
                .ToListAsync();

            ViewBag.Contracts = new SelectList(activeContracts, "ContractId", "Client.Name");
            var exchangeRate = await _currencyService.GetUsdToZarRateAsync();
            ViewBag.ExchangeRate = exchangeRate;

            return View();
        }

        // POST: ServiceRequests/Create
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ContractId,Description,AmountUSD")] ServiceRequest serviceRequest)
        {
            var contract = await _context.Contracts.FindAsync(serviceRequest.ContractId);

            if (contract == null)
            {
                ModelState.AddModelError("ContractId", "Invalid contract selected.");
                await PopulateCreateView();
                return View(serviceRequest);
            }

            if (!contract.CanCreateServiceRequest())
            {
                ModelState.AddModelError(string.Empty,
                    $"Cannot create service request. Contract is {contract.Status}. Only Active contracts can have service requests.");
                await PopulateCreateView();
                return View(serviceRequest);
            }

            var exchangeRate = await _currencyService.GetUsdToZarRateAsync();
            serviceRequest.ExchangeRateUsed = exchangeRate;
            serviceRequest.AmountZAR = serviceRequest.AmountUSD * exchangeRate;
            serviceRequest.CreatedAt = DateTime.Now;
            serviceRequest.Status = RequestStatus.Pending;

            if (ModelState.IsValid)
            {
                _context.Add(serviceRequest);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created new service request (ID: {RequestId})", serviceRequest.ServiceRequestId);
                TempData["SuccessMessage"] = "Service request created successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateCreateView();
            return View(serviceRequest);
        }

        // GET: ServiceRequests/Accept/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Accept(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceRequest = await _context.ServiceRequests
                .Include(s => s.Contract)
                .ThenInclude(c => c!.Client)
                .FirstOrDefaultAsync(s => s.ServiceRequestId == id);

            if (serviceRequest == null)
            {
                return NotFound();
            }

            if (serviceRequest.Status != RequestStatus.Pending)
            {
                TempData["ErrorMessage"] = "This service request has already been processed.";
                return RedirectToAction(nameof(Index));
            }

            return View(serviceRequest);
        }

        // POST: ServiceRequests/Accept/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id, string? adminComments)
        {
            var serviceRequest = await _context.ServiceRequests.FindAsync(id);

            if (serviceRequest == null)
            {
                return NotFound();
            }

            if (serviceRequest.Status != RequestStatus.Pending)
            {
                TempData["ErrorMessage"] = "This service request has already been processed.";
                return RedirectToAction(nameof(Index));
            }

            serviceRequest.Status = RequestStatus.Accepted;
            serviceRequest.AdminResponseDate = DateTime.Now;
            serviceRequest.AdminComments = adminComments;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Service request {RequestId} was ACCEPTED by admin", serviceRequest.ServiceRequestId);
            TempData["SuccessMessage"] = "Service request has been ACCEPTED!";

            return RedirectToAction(nameof(Index));
        }

        // GET: ServiceRequests/Deny/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Deny(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceRequest = await _context.ServiceRequests
                .Include(s => s.Contract)
                .ThenInclude(c => c!.Client)
                .FirstOrDefaultAsync(s => s.ServiceRequestId == id);

            if (serviceRequest == null)
            {
                return NotFound();
            }

            if (serviceRequest.Status != RequestStatus.Pending)
            {
                TempData["ErrorMessage"] = "This service request has already been processed.";
                return RedirectToAction(nameof(Index));
            }

            return View(serviceRequest);
        }

        // POST: ServiceRequests/Deny/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deny(int id, string? adminComments)
        {
            var serviceRequest = await _context.ServiceRequests.FindAsync(id);

            if (serviceRequest == null)
            {
                return NotFound();
            }

            if (serviceRequest.Status != RequestStatus.Pending)
            {
                TempData["ErrorMessage"] = "This service request has already been processed.";
                return RedirectToAction(nameof(Index));
            }

            serviceRequest.Status = RequestStatus.Denied;
            serviceRequest.AdminResponseDate = DateTime.Now;
            serviceRequest.AdminComments = adminComments;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Service request {RequestId} was DENIED by admin", serviceRequest.ServiceRequestId);
            TempData["SuccessMessage"] = "Service request has been DENIED.";

            return RedirectToAction(nameof(Index));
        }

        // GET: ServiceRequests/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceRequest = await _context.ServiceRequests.FindAsync(id);
            if (serviceRequest == null)
            {
                return NotFound();
            }

            var activeContracts = await _context.Contracts
                .Include(c => c.Client)
                .Where(c => c.Status == ContractStatus.Active && c.EndDate >= DateTime.Today)
                .ToListAsync();
            ViewBag.Contracts = new SelectList(activeContracts, "ContractId", "Client.Name", serviceRequest.ContractId);

            return View(serviceRequest);
        }

        // POST: ServiceRequests/Edit/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ServiceRequestId,ContractId,Description,AmountUSD,AmountZAR,Status,ExchangeRateUsed")] ServiceRequest serviceRequest)
        {
            if (id != serviceRequest.ServiceRequestId)
            {
                return NotFound();
            }

            var existingRequest = await _context.ServiceRequests.AsNoTracking().FirstOrDefaultAsync(s => s.ServiceRequestId == id);
            if (existingRequest != null && existingRequest.AmountUSD != serviceRequest.AmountUSD)
            {
                var exchangeRate = await _currencyService.GetUsdToZarRateAsync();
                serviceRequest.ExchangeRateUsed = exchangeRate;
                serviceRequest.AmountZAR = serviceRequest.AmountUSD * exchangeRate;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(serviceRequest);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Updated service request (ID: {RequestId})", serviceRequest.ServiceRequestId);
                    TempData["SuccessMessage"] = "Service request updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceRequestExists(serviceRequest.ServiceRequestId))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateEditView(serviceRequest.ServiceRequestId);
            return View(serviceRequest);
        }

        // GET: ServiceRequests/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceRequest = await _context.ServiceRequests
                .Include(s => s.Contract)
                .ThenInclude(c => c!.Client)
                .FirstOrDefaultAsync(m => m.ServiceRequestId == id);

            if (serviceRequest == null)
            {
                return NotFound();
            }

            return View(serviceRequest);
        }

        // POST: ServiceRequests/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var serviceRequest = await _context.ServiceRequests.FindAsync(id);
            if (serviceRequest != null)
            {
                _context.ServiceRequests.Remove(serviceRequest);
                _logger.LogWarning("Deleted service request (ID: {RequestId})", serviceRequest.ServiceRequestId);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Service request deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // API endpoint to get current exchange rate
        [HttpGet]
        public async Task<IActionResult> GetExchangeRate()
        {
            var rate = await _currencyService.GetUsdToZarRateAsync();
            return Json(new { rate });
        }

        private async Task PopulateCreateView()
        {
            var activeContracts = await _context.Contracts
                .Include(c => c.Client)
                .Where(c => c.Status == ContractStatus.Active && c.EndDate >= DateTime.Today)
                .ToListAsync();

            ViewBag.Contracts = new SelectList(activeContracts, "ContractId", "Client.Name");
            var exchangeRate = await _currencyService.GetUsdToZarRateAsync();
            ViewBag.ExchangeRate = exchangeRate;
        }

        private async Task PopulateEditView(int serviceRequestId)
        {
            var activeContracts = await _context.Contracts
                .Include(c => c.Client)
                .Where(c => c.Status == ContractStatus.Active && c.EndDate >= DateTime.Today)
                .ToListAsync();

            var serviceRequest = await _context.ServiceRequests.FindAsync(serviceRequestId);
            ViewBag.Contracts = new SelectList(activeContracts, "ContractId", "Client.Name", serviceRequest?.ContractId);
        }

        private bool ServiceRequestExists(int id)
        {
            return _context.ServiceRequests.Any(e => e.ServiceRequestId == id);
        }
    }
}