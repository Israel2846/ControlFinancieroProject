using ControlFinancieroProject.Models;
using ControlFinancieroProject.Services;
using Microsoft.AspNetCore.Mvc;

namespace ControlFinancieroProject.Controllers
{
    public class TransaccionController : Controller
    {
        private readonly ITransactionService _transactionService;
        private readonly ITransactionReportService _transactionReportService;

        public TransaccionController(
            ITransactionService transactionService,
            ITransactionReportService transactionReportService)
        {
            _transactionService = transactionService;
            _transactionReportService = transactionReportService;
        }

        public async Task<IActionResult> Index(string date, string q)
        {
            var model = await _transactionService.GetIndexViewModelAsync(date, q);
            return View(model);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var transaccion = await _transactionService.GetByIdAsync(id.Value);
            if (transaccion is null)
            {
                return NotFound();
            }

            return View(transaccion);
        }

        public async Task<IActionResult> Create()
        {
            var model = await _transactionService.GetCreateFormAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TransactionFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await _transactionService.PopulateFormOptionsAsync(model);
                return View(model);
            }

            var result = await _transactionService.CreateAsync(model);
            if (!result.IsSuccess)
            {
                if (result.IsNotFound)
                {
                    return NotFound();
                }

                ModelState.AddModelError(result.ErrorField ?? string.Empty, result.ErrorMessage!);
                await _transactionService.PopulateFormOptionsAsync(model);
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var model = await _transactionService.GetEditFormAsync(id.Value);
            if (model is null)
            {
                return NotFound();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TransactionFormViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await _transactionService.PopulateFormOptionsAsync(model);
                return View(model);
            }

            var result = await _transactionService.UpdateAsync(id, model);
            if (!result.IsSuccess)
            {
                if (result.IsNotFound)
                {
                    return NotFound();
                }

                ModelState.AddModelError(result.ErrorField ?? string.Empty, result.ErrorMessage!);
                await _transactionService.PopulateFormOptionsAsync(model);
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var transaccion = await _transactionService.GetByIdAsync(id.Value);
            if (transaccion is null)
            {
                return NotFound();
            }

            return View(transaccion);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var deleted = await _transactionService.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> MonthlyReport(int? year, int? month)
        {
            var model = await _transactionReportService.GetMonthlyReportAsync(year, month);
            return View(model);
        }

        public async Task<IActionResult> Comparative(int? year1, int? month1, int? year2, int? month2)
        {
            var model = await _transactionReportService.GetComparativeReportAsync(year1, month1, year2, month2);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecalculateAll()
        {
            await _transactionService.RecalculateAllAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
