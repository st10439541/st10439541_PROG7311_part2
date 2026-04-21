using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using St10439541_PROG7311_P2.Data;
using St10439541_PROG7311_P2.Models;
using St10439541_PROG7311_P2.Services;

namespace St10439541_PROG7311_P2.Controllers
{
    public class ContractsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileValidationService _fileValidationService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ContractsController> _logger;
        private readonly IUserAuthorizationService _userAuthService;

        public ContractsController(
            ApplicationDbContext context,
            IFileValidationService fileValidationService,
            IWebHostEnvironment webHostEnvironment,
            ILogger<ContractsController> logger,
            IUserAuthorizationService userAuthService)
        {
            _context = context;
            _fileValidationService = fileValidationService;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _userAuthService = userAuthService;
        }

        // GET: Contracts (Both Admin and Client can view, but filtered)
        [Authorize]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, ContractStatus? status)
        {
            var query = _context.Contracts
                .Include(c => c.Client)
                .AsQueryable();

            // If not admin, only show contracts for their client
            if (!User.IsInRole("Admin"))
            {
                var clientId = await _userAuthService.GetCurrentUserClientId();
                if (clientId.HasValue)
                {
                    query = query.Where(c => c.ClientId == clientId.Value);
                }
            }

            // Apply filters using LINQ
            if (startDate.HasValue)
            {
                query = query.Where(c => c.StartDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(c => c.EndDate <= endDate.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(c => c.Status == status.Value);
            }

            var contracts = await query.ToListAsync();

            // Store filter values for the view
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedStatus = status;

            return View(contracts);
        }

        // GET: Contracts/Details/5
        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contract = await _context.Contracts
                .Include(c => c.Client)
                .Include(c => c.ServiceRequests)
                .FirstOrDefaultAsync(m => m.ContractId == id);

            if (contract == null)
            {
                return NotFound();
            }

            // Check if user has access to this contract
            if (!User.IsInRole("Admin"))
            {
                var clientId = await _userAuthService.GetCurrentUserClientId();
                if (!clientId.HasValue || contract.ClientId != clientId.Value)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            return View(contract);
        }

        // GET: Contracts/Create - Admin only
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            // Get all clients for the dropdown
            var clients = await _context.Clients.ToListAsync();
            ViewBag.Clients = new SelectList(clients, "ClientId", "Name");
            return View();
        }

        // POST: Contracts/Create - Admin only
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClientId,StartDate,EndDate,ServiceLevel,TermsAndConditions")] Contract contract, IFormFile? PdfFile)
        {
            // Repopulate ViewBag.Clients in case of error
            var clients = await _context.Clients.ToListAsync();
            ViewBag.Clients = new SelectList(clients, "ClientId", "Name", contract.ClientId);

            // Set initial status to PendingClientSignature (waiting for client to sign)
            contract.Status = ContractStatus.PendingClientSignature;
            contract.IsSignedByClient = false;

            // Handle file upload (PDF from admin - contract document)
            if (PdfFile != null && PdfFile.Length > 0)
            {
                var validation = _fileValidationService.ValidatePdfFile(PdfFile);
                if (!validation.IsValid)
                {
                    ModelState.AddModelError("PdfFile", validation.ErrorMessage);
                    return View(contract);
                }

                // Save the file
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "contracts");
                Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = $"{Guid.NewGuid()}_{PdfFile.FileName}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await PdfFile.CopyToAsync(stream);
                }

                contract.PdfFilePath = $"/uploads/contracts/{uniqueFileName}";
            }

            if (ModelState.IsValid)
            {
                _context.Add(contract);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created new contract (ID: {ContractId}) for Client {ClientId} - Status: PendingClientSignature", contract.ContractId, contract.ClientId);
                TempData["SuccessMessage"] = "Contract created successfully! Waiting for client signature.";
                return RedirectToAction(nameof(Index));
            }

            return View(contract);
        }

        // GET: Contracts/Sign/5 - Client signs the contract
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Sign(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contract = await _context.Contracts
                .Include(c => c.Client)
                .FirstOrDefaultAsync(m => m.ContractId == id);

            if (contract == null)
            {
                return NotFound();
            }

            // Check if this contract belongs to the logged-in client
            var clientId = await _userAuthService.GetCurrentUserClientId();
            if (!clientId.HasValue || contract.ClientId != clientId.Value)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Check if contract can be signed
            if (!contract.CanBeSigned())
            {
                TempData["ErrorMessage"] = "This contract cannot be signed. It may already be signed or not in pending status.";
                return RedirectToAction(nameof(Index));
            }

            return View(contract);
        }

        // POST: Contracts/Sign/5 - Client confirms signature
        [HttpPost]
        [Authorize(Roles = "User")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sign(int id, IFormFile? SignedPdfFile)
        {
            var contract = await _context.Contracts.FindAsync(id);

            if (contract == null)
            {
                return NotFound();
            }

            // Verify ownership
            var clientId = await _userAuthService.GetCurrentUserClientId();
            if (!clientId.HasValue || contract.ClientId != clientId.Value)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (!contract.CanBeSigned())
            {
                TempData["ErrorMessage"] = "This contract cannot be signed.";
                return RedirectToAction(nameof(Index));
            }

            // Handle signed PDF upload from client
            if (SignedPdfFile != null && SignedPdfFile.Length > 0)
            {
                var validation = _fileValidationService.ValidatePdfFile(SignedPdfFile);
                if (!validation.IsValid)
                {
                    ModelState.AddModelError("SignedPdfFile", validation.ErrorMessage);
                    return View(contract);
                }

                // Save the signed PDF
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "contracts");
                Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = $"{Guid.NewGuid()}_signed_{SignedPdfFile.FileName}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await SignedPdfFile.CopyToAsync(stream);
                }

                // Update contract with signed PDF and change status to Active
                contract.PdfFilePath = $"/uploads/contracts/{uniqueFileName}";
                contract.IsSignedByClient = true;
                contract.SignatureDate = DateTime.Now;
                contract.Status = ContractStatus.Active;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Contract {ContractId} was signed by client {ClientId}", contract.ContractId, contract.ClientId);
                TempData["SuccessMessage"] = "Contract signed successfully! It is now active.";
            }
            else
            {
                TempData["ErrorMessage"] = "Please upload the signed PDF document.";
                return View(contract);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Contracts/Edit/5 - Admin Only
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null)
            {
                return NotFound();
            }

            // Get all clients for the dropdown
            var clients = await _context.Clients.ToListAsync();
            ViewBag.Clients = new SelectList(clients, "ClientId", "Name", contract.ClientId);

            return View(contract);
        }

        // POST: Contracts/Edit/5 - Admin Only
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ContractId,ClientId,StartDate,EndDate,Status,ServiceLevel,TermsAndConditions,PdfFilePath,IsSignedByClient,SignatureDate")] Contract contract, IFormFile? PdfFile)
        {
            if (id != contract.ContractId)
            {
                return NotFound();
            }

            // Repopulate ViewBag.Clients in case of error
            var clients = await _context.Clients.ToListAsync();
            ViewBag.Clients = new SelectList(clients, "ClientId", "Name", contract.ClientId);

            // Handle file upload if new file is provided
            if (PdfFile != null && PdfFile.Length > 0)
            {
                var validation = _fileValidationService.ValidatePdfFile(PdfFile);
                if (!validation.IsValid)
                {
                    ModelState.AddModelError("PdfFile", validation.ErrorMessage);
                    return View(contract);
                }

                // Delete old file if exists
                if (!string.IsNullOrEmpty(contract.PdfFilePath))
                {
                    string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, contract.PdfFilePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Save new file
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "contracts");
                Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = $"{Guid.NewGuid()}_{PdfFile.FileName}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await PdfFile.CopyToAsync(stream);
                }

                contract.PdfFilePath = $"/uploads/contracts/{uniqueFileName}";
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(contract);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Updated contract (ID: {ContractId})", contract.ContractId);
                    TempData["SuccessMessage"] = "Contract updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContractExists(contract.ContractId))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(contract);
        }

        // GET: Contracts/Delete/5 - Admin Only
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contract = await _context.Contracts
                .Include(c => c.Client)
                .FirstOrDefaultAsync(m => m.ContractId == id);

            if (contract == null)
            {
                return NotFound();
            }

            return View(contract);
        }

        // POST: Contracts/Delete/5 - Admin Only
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract != null)
            {
                // Delete the PDF file if it exists
                if (!string.IsNullOrEmpty(contract.PdfFilePath))
                {
                    string filePath = Path.Combine(_webHostEnvironment.WebRootPath, contract.PdfFilePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                        _logger.LogInformation("Deleted PDF file for contract (ID: {ContractId})", contract.ContractId);
                    }
                }

                _context.Contracts.Remove(contract);
                _logger.LogWarning("Deleted contract (ID: {ContractId})", contract.ContractId);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Contract deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Contracts/DownloadPdf/5
        [Authorize]
        public async Task<IActionResult> DownloadPdf(int id)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null || string.IsNullOrEmpty(contract.PdfFilePath))
            {
                return NotFound();
            }

            // Check access - only admin or the client who owns the contract can download
            if (!User.IsInRole("Admin"))
            {
                var clientId = await _userAuthService.GetCurrentUserClientId();
                if (!clientId.HasValue || contract.ClientId != clientId.Value)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, contract.PdfFilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            string fileName = Path.GetFileName(contract.PdfFilePath);

            return File(fileBytes, "application/pdf", fileName);
        }

        private bool ContractExists(int id)
        {
            return _context.Contracts.Any(e => e.ContractId == id);
        }
    }
}