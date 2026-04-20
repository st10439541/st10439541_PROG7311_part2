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
using Microsoft.AspNetCore.Authorization;

namespace St10439541_PROG7311_P2.Controllers
{
    public class ContractsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileValidationService _fileValidationService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ContractsController> _logger;

        public ContractsController(
            ApplicationDbContext context,
            IFileValidationService fileValidationService,
            IWebHostEnvironment webHostEnvironment,
            ILogger<ContractsController> logger)
        {
            _context = context;
            _fileValidationService = fileValidationService;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // GET: Contracts
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, ContractStatus? status)
        {
            var query = _context.Contracts
                .Include(c => c.Client)
                .AsQueryable();

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

            return View(contract);
        }

        // GET: Contracts/Create
        public async Task<IActionResult> Create()
        {
            // Get all clients for the dropdown
            var clients = await _context.Clients.ToListAsync();
            ViewBag.Clients = new SelectList(clients, "ClientId", "Name");
            return View();
        }

        // POST: Contracts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClientId,StartDate,EndDate,Status,ServiceLevel,TermsAndConditions")] Contract contract, IFormFile? PdfFile)
        {
            // Repopulate ViewBag.Clients in case of error
            var clients = await _context.Clients.ToListAsync();
            ViewBag.Clients = new SelectList(clients, "ClientId", "Name", contract.ClientId);

            // Handle file upload
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
                _logger.LogInformation("Created new contract (ID: {ContractId}) for Client {ClientId}", contract.ContractId, contract.ClientId);
                return RedirectToAction(nameof(Index));
            }

            return View(contract);
        }

        // GET: Contracts/Edit/5
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

        // POST: Contracts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ContractId,ClientId,StartDate,EndDate,Status,ServiceLevel,TermsAndConditions,PdfFilePath")] Contract contract, IFormFile? PdfFile)
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

        // GET: Contracts/Delete/5
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

        // POST: Contracts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract != null)
            {
                _context.Contracts.Remove(contract);
                _logger.LogWarning("Deleted contract (ID: {ContractId})", contract.ContractId);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Contracts/DownloadPdf/5
        public async Task<IActionResult> DownloadPdf(int id)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null || string.IsNullOrEmpty(contract.PdfFilePath))
            {
                return NotFound();
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