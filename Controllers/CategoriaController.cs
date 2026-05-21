using ControlFinancieroProject.Models;
using ControlFinancieroProject.Services;
using Microsoft.AspNetCore.Mvc;

namespace ControlFinancieroProject.Controllers
{
    public class CategoriaController : Controller
    {
        private readonly ICategoryService _categoryService;

        public CategoriaController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        public async Task<IActionResult> Index()
        {
            var categorias = await _categoryService.GetAllAsync();
            return View(categorias);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var categoria = await _categoryService.GetByIdAsync(id.Value, includeTransactions: true);
            if (categoria is null)
            {
                return NotFound();
            }

            return View(categoria);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Categoria categoria)
        {
            if (!ModelState.IsValid)
            {
                return View(categoria);
            }

            await _categoryService.CreateAsync(categoria);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var categoria = await _categoryService.GetByIdAsync(id.Value);
            if (categoria is null)
            {
                return NotFound();
            }

            return View(categoria);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Categoria categoria)
        {
            if (id != categoria.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(categoria);
            }

            var updated = await _categoryService.UpdateAsync(categoria);
            if (!updated)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var categoria = await _categoryService.GetByIdAsync(id.Value);
            if (categoria is null)
            {
                return NotFound();
            }

            return View(categoria);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var deleted = await _categoryService.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
