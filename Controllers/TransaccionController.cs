using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlFinancieroProject.Data; // ajusta al namespace de tu DbContext
using ControlFinancieroProject.Models;

namespace ControlFinancieroProject.Controllers
{
    public class TransaccionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TransaccionController> _logger;

        public TransaccionController(ApplicationDbContext context, ILogger<TransaccionController> logger) 
        { 
            _context = context; 
            _logger = logger;
        }

        // GET: Transacciones
        // Ahora acepta opcionalmente `date` (yyyy-MM-dd) y `q` (texto de búsqueda)
        // Devuelve SOLO las transacciones del día solicitado (evita enviar todo al cliente).
        public async Task<IActionResult> Index(string date, string q)
        {
            DateTime selectedDate;
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out selectedDate))
            {
                selectedDate = DateTime.Today;
            }

            var start = selectedDate.Date;
            var end = start.AddDays(1);

            // Consultar únicamente las transacciones del día, incluyendo categoría
            var query = _context.Transaccion
                .AsNoTracking()
                .Include(t => t.Categoria)
                .Where(t => t.Fecha >= start && t.Fecha < end);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var qNorm = q.Trim();
                // Usar EF.Functions.Like para búsqueda más compatible con el proveedor
                query = query.Where(t => t.Descripcion != null && EF.Functions.Like(t.Descripcion, $"%{qNorm}%"));
            }

            var transacciones = await query
                .OrderByDescending(t => t.Fecha)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            return View(transacciones);
        }

        // GET: Transacciones/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Transaccion == null) return NotFound();
            var transaccion = await _context.Transaccion
                .AsNoTracking()
                .Include(t => t.Categoria)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (transaccion == null) return NotFound();
            return View(transaccion);
        }

        // GET: Transacciones/Create
        public async Task<IActionResult> Create()
        {
            // Enviar la lista completa de categorías para renderizar opciones con data-tipo
            ViewData["Categorias"] = await _context.Categoria.AsNoTracking().OrderBy(c => c.Descripcion).ToListAsync();
            return View();
        }

        // POST: Transacciones/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Transaccion transaccion)
        {
            _logger.LogInformation("Transaccion Create POST invoked. ModelState.IsValid={IsValid}", ModelState.IsValid);

            if (!ModelState.IsValid)
            {
                // Logear errores de ModelState para depuración
                foreach (var kv in ModelState)
                {
                    if (kv.Value.Errors.Any())
                    {
                        _logger.LogWarning("ModelState error for {Key}: {Errors}", kv.Key, string.Join("; ", kv.Value.Errors.Select(e => e.ErrorMessage)));
                    }
                }

                ViewData["Categorias"] = await _context.Categoria.AsNoTracking().OrderBy(c => c.Descripcion).ToListAsync();
                return View(transaccion);
            }

            var categoria = await _context.Categoria.FindAsync(transaccion.CategoriaId);
            if (categoria == null)
            {
                ModelState.AddModelError("CategoriaId", "Categoría inválida.");
                _logger.LogWarning("Create: Categoría inválida id={CategoriaId}", transaccion.CategoriaId);
                ViewData["Categorias"] = await _context.Categoria.AsNoTracking().OrderBy(c => c.Descripcion).ToListAsync();
                return View(transaccion);
            }

            // calcular saldo global (o por categoría según tu política)
            var ultimo = await _context.Transaccion
                .OrderByDescending(t => t.Fecha).ThenByDescending(t => t.Id)
                .FirstOrDefaultAsync();

            transaccion.SaldoAnterior = ultimo?.SaldoNuevo ?? 0m;
            if (categoria.Tipo == TipoCategoria.Ingreso)
                transaccion.SaldoNuevo = transaccion.SaldoAnterior + transaccion.Monto;
            else
                transaccion.SaldoNuevo = transaccion.SaldoAnterior - transaccion.Monto;

            _context.Add(transaccion);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Transaccion created id={Id}", transaccion.Id);
            return RedirectToAction(nameof(Index));
        }

        // GET: Transacciones/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Transaccion == null) return NotFound();
            var transaccion = await _context.Transaccion.FindAsync(id);
            if (transaccion == null) return NotFound();

            ViewData["Categorias"] = await _context.Categoria.AsNoTracking().OrderBy(c => c.Descripcion).ToListAsync();
            return View(transaccion);
        }

        // POST: Transacciones/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Transaccion transaccion)
        {
            if (id != transaccion.Id) return NotFound();
            if (!ModelState.IsValid)
            {
                ViewData["Categorias"] = await _context.Categoria.AsNoTracking().OrderBy(c => c.Descripcion).ToListAsync();
                return View(transaccion);
            }

            var categoria = await _context.Categoria.FindAsync(transaccion.CategoriaId);
            if (categoria == null)
            {
                ModelState.AddModelError("CategoriaId", "Categoría inválida.");
                ViewData["Categorias"] = await _context.Categoria.AsNoTracking().OrderBy(c => c.Descripcion).ToListAsync();
                return View(transaccion);
            }

            // buscar la transacción anterior respecto a la fecha/id a nivel global.
            var anterior = await _context.Transaccion
                .Where(t => t.Id != transaccion.Id)
                .Where(t => t.Fecha < transaccion.Fecha || (t.Fecha == transaccion.Fecha && t.Id < transaccion.Id))
                .OrderByDescending(t => t.Fecha).ThenByDescending(t => t.Id)
                .FirstOrDefaultAsync();

            transaccion.SaldoAnterior = anterior?.SaldoNuevo ?? 0m;

            if (categoria.Tipo == TipoCategoria.Ingreso)
            {
                transaccion.SaldoNuevo = transaccion.SaldoAnterior + transaccion.Monto;
            }
            else
            {
                transaccion.SaldoNuevo = transaccion.SaldoAnterior - transaccion.Monto;
            }

            try
            {
                var entity = await _context.Transaccion.FindAsync(id);
                if (entity == null) return NotFound();

                entity.Fecha = transaccion.Fecha;
                entity.Monto = transaccion.Monto;
                entity.Descripcion = transaccion.Descripcion;
                entity.CategoriaId = transaccion.CategoriaId;
                entity.SaldoAnterior = transaccion.SaldoAnterior;
                entity.SaldoNuevo = transaccion.SaldoNuevo;

                _context.Update(entity);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Transaccion.AnyAsync(e => e.Id == transaccion.Id))
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Transacciones/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Transaccion == null) return NotFound();
            var transaccion = await _context.Transaccion
                .AsNoTracking()
                .Include(t => t.Categoria)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (transaccion == null) return NotFound();
            return View(transaccion);
        }

        // POST: Transacciones/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Transaccion == null) return Problem("Entity set 'ApplicationDbContext.Transaccion' is null.");
            var transaccion = await _context.Transaccion.FindAsync(id);
            if (transaccion != null)
            {
                _context.Transaccion.Remove(transaccion);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Añadir este método dentro de la clase TransaccionController
        public async Task<IActionResult> MonthlyReport(int? year, int? month)
        {
            var now = DateTime.Now;
            var y = year ?? now.Year;
            var m = month ?? now.Month;
            var firstDay = new DateTime(y, m, 1);
            var lastDay = firstDay.AddMonths(1).AddTicks(-1);

            var transacciones = await _context.Transaccion
                .AsNoTracking()
                .Include(t => t.Categoria)
                .Where(t => t.Fecha >= firstDay && t.Fecha <= lastDay)
                .OrderByDescending(t => t.Fecha).ThenByDescending(t => t.Id)
                .ToListAsync();

            // Datos diarios (gastos por día)
            var daysInMonth = DateTime.DaysInMonth(y, m);
            var dailyLabels = new List<string>(daysInMonth);
            var dailyExpenses = new List<decimal>(daysInMonth);
            for (int d = 1; d <= daysInMonth; d++)
            {
                var date = new DateTime(y, m, d);
                dailyLabels.Add(date.ToString("yyyy-MM-dd"));
                var totalDayExpense = transacciones
                    .Where(t => t.Categoria != null && t.Categoria.Tipo == TipoCategoria.Gasto && t.Fecha.Date == date.Date)
                    .Sum(t => t.Monto);
                dailyExpenses.Add(totalDayExpense);
            }

            // Agrupaciones por categoría
            var expenseGroups = transacciones
                .Where(t => t.Categoria != null && t.Categoria.Tipo == TipoCategoria.Gasto)
                .GroupBy(t => t.Categoria!.Descripcion)
                .Select(g => new { Label = g.Key ?? "Sin categoría", Value = g.Sum(t => t.Monto) })
                .OrderByDescending(g => g.Value)
                .ToList();

            var incomeGroups = transacciones
                .Where(t => t.Categoria != null && t.Categoria.Tipo == TipoCategoria.Ingreso)
                .GroupBy(t => t.Categoria!.Descripcion)
                .Select(g => new { Label = g.Key ?? "Sin categoría", Value = g.Sum(t => t.Monto) })
                .OrderByDescending(g => g.Value)
                .ToList();

            var model = new MonthlyReportViewModel
            {
                Year = y,
                Month = m,
                Transacciones = transacciones,
                DailyLabels = dailyLabels,
                DailyExpenses = dailyExpenses,
                ExpenseCategoryLabels = expenseGroups.Select(g => g.Label).ToList(),
                ExpenseCategoryValues = expenseGroups.Select(g => g.Value).ToList(),
                IncomeCategoryLabels = incomeGroups.Select(g => g.Label).ToList(),
                IncomeCategoryValues = incomeGroups.Select(g => g.Value).ToList(),
                TotalExpense = expenseGroups.Sum(g => g.Value),
                TotalIncome = incomeGroups.Sum(g => g.Value),
                CurrentBalance = (await _context.Transaccion.OrderByDescending(t => t.Fecha).ThenByDescending(t => t.Id).FirstOrDefaultAsync())?.SaldoNuevo ?? 0m
            };

            return View(model);
        }

        // GET: Transacciones/Comparative
        public async Task<IActionResult> Comparative(int? year1, int? month1, int? year2, int? month2)
        {
            var now = DateTime.Now;
            var y1 = year1 ?? now.Year;
            var m1 = month1 ?? now.Month;
            var y2 = year2 ?? now.Year;
            var m2 = month2 ?? now.Month;

            var first1 = new DateTime(y1, m1, 1);
            var last1 = first1.AddMonths(1).AddTicks(-1);
            var first2 = new DateTime(y2, m2, 1);
            var last2 = first2.AddMonths(1).AddTicks(-1);

            var t1 = await _context.Transaccion
                .AsNoTracking()
                .Include(t => t.Categoria)
                .Where(t => t.Fecha >= first1 && t.Fecha <= last1)
                .ToListAsync();

            var t2 = await _context.Transaccion
                .AsNoTracking()
                .Include(t => t.Categoria)
                .Where(t => t.Fecha >= first2 && t.Fecha <= last2)
                .ToListAsync();

            // Construir listas de categorías a partir de las transacciones (solo las categorías con datos)
            var expenseCats = t1.Concat(t2)
                .Where(x => x.Categoria != null && x.Categoria.Tipo == TipoCategoria.Gasto)
                .Select(x => x.Categoria!.Descripcion ?? "-")
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            var incomeCats = t1.Concat(t2)
                .Where(x => x.Categoria != null && x.Categoria.Tipo == TipoCategoria.Ingreso)
                .Select(x => x.Categoria!.Descripcion ?? "-")
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            var expenseVals1 = expenseCats.Select(cat => t1.Where(x => (x.Categoria?.Descripcion ?? "-") == cat).Sum(x => x.Monto)).ToList();
            var expenseVals2 = expenseCats.Select(cat => t2.Where(x => (x.Categoria?.Descripcion ?? "-") == cat).Sum(x => x.Monto)).ToList();

            var incomeVals1 = incomeCats.Select(cat => t1.Where(x => (x.Categoria?.Descripcion ?? "-") == cat).Sum(x => x.Monto)).ToList();
            var incomeVals2 = incomeCats.Select(cat => t2.Where(x => (x.Categoria?.Descripcion ?? "-") == cat).Sum(x => x.Monto)).ToList();

            // Fallback general (opcional): todas las categorías registradas
            var allCats = await _context.Categoria.AsNoTracking().OrderBy(c => c.Descripcion).Select(c => c.Descripcion ?? "-").ToListAsync();
            var vals1 = allCats.Select(cat => t1.Where(x => (x.Categoria?.Descripcion ?? "-") == cat).Sum(x => x.Monto)).ToList();
            var vals2 = allCats.Select(cat => t2.Where(x => (x.Categoria?.Descripcion ?? "-") == cat).Sum(x => x.Monto)).ToList();

            var model = new ComparativeReportViewModel
            {
                Year1 = y1,
                Month1 = m1,
                Year2 = y2,
                Month2 = m2,

                // generales (fallback)
                CategoryLabels = allCats,
                ValuesMonth1 = vals1,
                ValuesMonth2 = vals2,

                // gastos/ingresos desde transacciones con datos
                ExpenseCategoryLabels = expenseCats,
                ExpenseValuesMonth1 = expenseVals1,
                ExpenseValuesMonth2 = expenseVals2,

                IncomeCategoryLabels = incomeCats,
                IncomeValuesMonth1 = incomeVals1,
                IncomeValuesMonth2 = incomeVals2,

                TotalMonth1 = vals1.Sum(),
                TotalMonth2 = vals2.Sum(),

                TransaccionesMonth1 = t1.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id),
                TransaccionesMonth2 = t2.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id)
            };

            return View(model);
        }

        // Acción pública (puedes protegerla con autorización en producción)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecalculateAll()
        {
            _logger.LogInformation("RecalculateAll invoked by user.");
            await RecalculateSaldosFromAsync(DateTime.MinValue, null); // fuerza recálculo desde el principio
            _logger.LogInformation("RecalculateAll finished.");
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Recalcula SaldoAnterior/SaldoNuevo de todas las transacciones afectadas
        /// empezando desde la posición inmediatamente posterior a (fromDate, fromId).
        /// Si fromDate == DateTime.MinValue y fromId == null recalcula todo.
        /// </summary>
        private async Task RecalculateSaldosFromAsync(DateTime fromDate, int? fromId)
        {
            _logger.LogInformation("RecalculateSaldosFromAsync start fromDate={FromDate} fromId={FromId}", fromDate, fromId);

            // encontrar la transacción previa (la última anterior a la posición de inicio)
            var previousQuery = _context.Transaccion.AsQueryable();
            if (fromId.HasValue)
            {
                previousQuery = previousQuery.Where(t => t.Fecha < fromDate || (t.Fecha == fromDate && t.Id < fromId.Value));
            }
            else if (fromDate > DateTime.MinValue)
            {
                previousQuery = previousQuery.Where(t => t.Fecha < fromDate);
            }
            else
            {
                previousQuery = previousQuery.Where(t => false); // forzar null si queremos desde inicio
            }

            var previous = await previousQuery
                .OrderByDescending(t => t.Fecha).ThenByDescending(t => t.Id)
                .FirstOrDefaultAsync();

            var running = previous?.SaldoNuevo ?? 0m;
            _logger.LogInformation("Starting running balance={Running} from previous id={PrevId}", running, previous?.Id);

            // obtener todas las transacciones a partir de la siguiente posición, en orden cronológico
            var affectedQuery = _context.Transaccion
                .Include(t => t.Categoria)
                .AsQueryable();

            if (fromDate > DateTime.MinValue || fromId.HasValue)
            {
                var prevDate = previous?.Fecha ?? DateTime.MinValue;
                var prevId = previous?.Id ?? int.MinValue;
                affectedQuery = affectedQuery.Where(t => t.Fecha > prevDate || (t.Fecha == prevDate && t.Id > prevId));
            }

            var affected = await affectedQuery
                .OrderBy(t => t.Fecha).ThenBy(t => t.Id)
                .ToListAsync();

            if (!affected.Any())
            {
                _logger.LogInformation("No transacciones afectadas. Nothing to update.");
                return;
            }

            var updatedCount = 0;
            foreach (var t in affected)
            {
                var oldAnterior = t.SaldoAnterior;
                var oldNuevo = t.SaldoNuevo;

                t.SaldoAnterior = running;
                if (t.Categoria != null && t.Categoria.Tipo == TipoCategoria.Ingreso)
                    running = t.SaldoNuevo = running + t.Monto;
                else
                    running = t.SaldoNuevo = running - t.Monto;

                if (t.SaldoAnterior != oldAnterior || t.SaldoNuevo != oldNuevo)
                    updatedCount++;
            }

            var saved = await _context.SaveChangesAsync();
            _logger.LogInformation("RecalculateSaldosFromAsync finished. AffectedEntities={Affected}, SaveChangesReturned={Saved}", updatedCount, saved);
        }
    }
}
