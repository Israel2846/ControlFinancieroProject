using ControlFinancieroProject.Data;
using ControlFinancieroProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlFinancieroProject.Controllers
{
    public class CategoriaController : Controller
    {
        private readonly ApplicationDbContext _context;
        public CategoriaController(ApplicationDbContext context) => _context = context;

        // GET: Categorias
        public async Task<IActionResult> Index()
        {
            var categorias = await _context.Categoria
                .AsNoTracking()
                .OrderBy(c => c.Descripcion)
                .ToListAsync();
            return View(categorias);
        }

        // GET: Categorias/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Categoria == null) return NotFound();
            var categoria = await _context.Categoria
                .AsNoTracking()
                .Include(c => c.Transacciones)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (categoria == null) return NotFound();
            return View(categoria);
        }

        // GET: Categorias/Create
        public IActionResult Create() => View();

        // POST: Categorias/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Categoria categoria)
        {
            if (ModelState.IsValid)
            {
                _context.Add(categoria);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(categoria);
        }

        // GET: Categorias/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Categoria == null) return NotFound();
            var categoria = await _context.Categoria.FindAsync(id);
            if (categoria == null) return NotFound();
            return View(categoria);
        }

        // POST: Categorias/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Categoria categoria)
        {
            if (id != categoria.Id) return NotFound();
            if (!ModelState.IsValid) return View(categoria);
            try
            {
                _context.Update(categoria);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Categoria.AnyAsync(e => e.Id == categoria.Id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Categorias/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Categoria == null) return NotFound();
            var categoria = await _context.Categoria
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (categoria == null) return NotFound();
            return View(categoria);
        }

        // POST: Categorias/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Categoria == null) return Problem("Entity set 'ApplicationDbContext.Categoria'  is null.");
            var categoria = await _context.Categoria.FindAsync(id);
            if (categoria != null)
            {
                _context.Categoria.Remove(categoria);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
