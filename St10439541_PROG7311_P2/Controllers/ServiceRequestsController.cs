using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public async Task<IActionResult> Index()
        {
            // Include Contract and Client information for display
            var serviceRequests = await _context.ServiceRequests
                .Include(s => s.Contract)
                .ThenInclude(c => c!.Client)
                .ToListAsync();
            return View(serviceRequests);
        }

        // GET: ServiceRequests/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Include related Contract and Client information
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
        public async Task<IActionResult> Create()
        {
            // Get only active contracts for service request creation
            // Business rule: ServiceRequest cannot be created if Contract is Expired or On Hold
            var activeContracts = await _context.Contracts
                .Include(c => c.Client)
                .Where(c => c.Status == ContractStatus.Active && c.EndDate >= DateTime.Today)
                .ToListAsync();

            ViewBag.Contracts = new SelectList(activeContracts, "ContractId", "Client.Name");

            // Get current exchange rate for the view
            var exchangeRate = await _currencyService.GetUsdToZarRateAsync();
            ViewBag.ExchangeRate = exchangeRate;

            return View();
        }

        // POST: ServiceRequests/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ContractId,Description,AmountUSD")] ServiceRequest serviceRequest)
        {
            // Validate that contract exists and is active (workflow logic)
            var contract = await _context.Contracts.FindAsync(serviceRequest.ContractId);

            if (contract == null)
            {
                ModelState.AddModelError("ContractId", "Invalid contract selected.");
                await PopulateCreateView();
                return View(serviceRequest);
            }

            // Business rule: ServiceRequest cannot be created if Contract is Expired or On Hold
            if (!contract.CanCreateServiceRequest())
            {
                ModelState.AddModelError(string.Empty,
                    $"Cannot create service request. Contract is {contract.Status}. Only Active contracts can have service requests.");
                await PopulateCreateView();
                return View(serviceRequest);
            }

            // Additional check: Contract should not be expired by date
            if (contract.IsExpired())
            {
                ModelState.AddModelError(string.Empty,
                    "Cannot create service request. Contract has expired.");
                await PopulateCreateView();
                return View(serviceRequest);
            }

            // Get current exchange rate and calculate ZAR amount
            var exchangeRate = await _currencyService.GetUsdToZarRateAsync();
            serviceRequest.ExchangeRateUsed = exchangeRate;
            serviceRequest.AmountZAR = serviceRequest.AmountUSD * exchangeRate;
            serviceRequest.CreatedAt = DateTime.Now;
            serviceRequest.Status = RequestStatus.Pending;

            if (ModelState.IsValid)
            {
                _context.Add(serviceRequest);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created new service request (ID: {RequestId}) for Contract {ContractId} - Amount: ${AmountUSD} USD (R{AmountZAR} ZAR)",
                    serviceRequest.ServiceRequestId, serviceRequest.ContractId, serviceRequest.AmountUSD, serviceRequest.AmountZAR);
                return RedirectToAction(nameof(Index));
            }

            await PopulateCreateView();
            return View(serviceRequest);
        }

        // GET: ServiceRequests/Edit/5
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

            // Get active contracts for the dropdown
            var activeContracts = await _context.Contracts
                .Include(c => c.Client)
                .Where(c => c.Status == ContractStatus.Active && c.EndDate >= DateTime.Today)
                .ToListAsync();
            ViewBag.Contracts = new SelectList(activeContracts, "ContractId", "Client.Name", serviceRequest.ContractId);

            return View(serviceRequest);
        }

        // POST: ServiceRequests/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ServiceRequestId,ContractId,Description,AmountUSD,AmountZAR,Status,ExchangeRateUsed")] ServiceRequest serviceRequest)
        {
            if (id != serviceRequest.ServiceRequestId)
            {
                return NotFound();
            }

            // Verify the contract is still active for this edit
            var contract = await _context.Contracts.FindAsync(serviceRequest.ContractId);
            if (contract != null && !contract.CanCreateServiceRequest())
            {
                ModelState.AddModelError(string.Empty,
                    $"Cannot edit service request. Contract is {contract?.Status}. Only Active contracts can have service requests.");
                await PopulateEditView(serviceRequest.ServiceRequestId);
                return View(serviceRequest);
            }

            // Recalculate ZAR amount if USD amount changed
            var existingRequest = await _context.ServiceRequests.AsNoTracking().FirstOrDefaultAsync(s => s.ServiceRequestId == id);
            if (existingRequest != null && existingRequest.AmountUSD != serviceRequest.AmountUSD)
            {
                var exchangeRate = await _currencyService.GetUsdToZarRateAsync();
                serviceRequest.ExchangeRateUsed = exchangeRate;
                serviceRequest.AmountZAR = serviceRequest.AmountUSD * exchangeRate;
                _logger.LogInformation("Recalculated ZAR amount for Service Request {RequestId}: ${AmountUSD} USD * {Rate} = R{AmountZAR} ZAR",
                    serviceRequest.ServiceRequestId, serviceRequest.AmountUSD, exchangeRate, serviceRequest.AmountZAR);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(serviceRequest);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Updated service request (ID: {RequestId}) - Status: {Status}",
                        serviceRequest.ServiceRequestId, serviceRequest.Status);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceRequestExists(serviceRequest.ServiceRequestId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateEditView(serviceRequest.ServiceRequestId);
            return View(serviceRequest);
        }

        // GET: ServiceRequests/Delete/5
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var serviceRequest = await _context.ServiceRequests.FindAsync(id);
            if (serviceRequest != null)
            {
                _context.ServiceRequests.Remove(serviceRequest);
                _logger.LogWarning("Deleted service request (ID: {RequestId}) for Contract {ContractId}",
                    serviceRequest.ServiceRequestId, serviceRequest.ContractId);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // API endpoint to get current exchange rate (for AJAX calls from the create/edit page)
        [HttpGet]
        public async Task<IActionResult> GetExchangeRate()
        {
            var rate = await _currencyService.GetUsdToZarRateAsync();
            return Json(new { rate });
        }

        // Helper method to populate the Create view with required data
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

        // Helper method to populate the Edit view with required data
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