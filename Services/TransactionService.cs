using System.Globalization;
using ControlFinancieroProject.Data;
using ControlFinancieroProject.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlFinancieroProject.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITransactionBalanceService _balanceService;
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(
            ApplicationDbContext context,
            ITransactionBalanceService balanceService,
            ILogger<TransactionService> logger)
        {
            _context = context;
            _balanceService = balanceService;
            _logger = logger;
        }

        public async Task<TransactionIndexViewModel> GetIndexViewModelAsync(string? date, string? searchTerm)
        {
            var selectedDate = ParseDateOrToday(date);
            var start = selectedDate.Date;
            var end = start.AddDays(1);

            var query = _context.Transaccion
                .AsNoTracking()
                .Include(t => t.Categoria)
                .Where(t => t.Fecha >= start && t.Fecha < end);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalizedSearch = searchTerm.Trim();
                query = query.Where(t => EF.Functions.Like(t.Descripcion, $"%{normalizedSearch}%"));
            }

            var transacciones = await query
                .OrderByDescending(t => t.Fecha)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            return new TransactionIndexViewModel
            {
                SelectedDate = selectedDate,
                SearchTerm = searchTerm,
                Transacciones = transacciones,
                DailyIncome = transacciones
                    .Where(t => t.Categoria?.Tipo == TipoCategoria.Ingreso)
                    .Sum(t => t.Monto),
                DailyExpense = transacciones
                    .Where(t => t.Categoria?.Tipo == TipoCategoria.Gasto)
                    .Sum(t => t.Monto)
            };
        }

        public async Task<Transaccion?> GetByIdAsync(int id)
        {
            return await _context.Transaccion
                .AsNoTracking()
                .Include(t => t.Categoria)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<TransactionFormViewModel> GetCreateFormAsync()
        {
            var model = new TransactionFormViewModel
            {
                Fecha = DateTime.Today
            };

            await PopulateFormOptionsAsync(model);
            return model;
        }

        public async Task<TransactionFormViewModel?> GetEditFormAsync(int id)
        {
            var transaction = await _context.Transaccion
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction is null)
            {
                return null;
            }

            var model = new TransactionFormViewModel
            {
                Id = transaction.Id,
                Fecha = transaction.Fecha,
                Monto = transaction.Monto,
                Descripcion = transaction.Descripcion,
                CategoriaId = transaction.CategoriaId
            };

            await PopulateFormOptionsAsync(model);
            return model;
        }

        public async Task PopulateFormOptionsAsync(TransactionFormViewModel model)
        {
            var categorias = await _context.Categoria
                .AsNoTracking()
                .OrderBy(c => c.Descripcion)
                .Select(c => new CategoryOptionViewModel
                {
                    Id = c.Id,
                    Descripcion = c.Descripcion,
                    Tipo = c.Tipo
                })
                .ToListAsync();

            model.Categorias = categorias;

            if (model.CategoriaId > 0)
            {
                model.SelectedTipo = categorias
                    .Where(c => c.Id == model.CategoriaId)
                    .Select(c => c.Tipo)
                    .Cast<TipoCategoria?>()
                    .FirstOrDefault();
            }
        }

        public async Task<OperationResult> CreateAsync(TransactionFormViewModel model)
        {
            var categoria = await _context.Categoria
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == model.CategoriaId);

            if (categoria is null)
            {
                return OperationResult.Failure("Categoria invalida.", nameof(model.CategoriaId));
            }

            var balances = await _balanceService.CalculateAsync(model.Fecha, null, model.Monto, categoria.Tipo);

            var transaction = new Transaccion
            {
                Fecha = model.Fecha,
                Monto = model.Monto,
                Descripcion = model.Descripcion.Trim(),
                CategoriaId = model.CategoriaId,
                SaldoAnterior = balances.saldoAnterior,
                SaldoNuevo = balances.saldoNuevo
            };

            _context.Transaccion.Add(transaction);
            await _context.SaveChangesAsync();
            await _balanceService.RecalculateFromAsync(transaction.Fecha, transaction.Id);

            _logger.LogInformation("Transaction {TransactionId} created.", transaction.Id);
            return OperationResult.Success();
        }

        public async Task<OperationResult> UpdateAsync(int id, TransactionFormViewModel model)
        {
            if (id != model.Id)
            {
                return OperationResult.NotFound();
            }

            var transaction = await _context.Transaccion.FindAsync(id);
            if (transaction is null)
            {
                return OperationResult.NotFound();
            }

            var categoria = await _context.Categoria
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == model.CategoriaId);

            if (categoria is null)
            {
                return OperationResult.Failure("Categoria invalida.", nameof(model.CategoriaId));
            }

            var previousPosition = (transaction.Fecha, transaction.Id);

            transaction.Fecha = model.Fecha;
            transaction.Monto = model.Monto;
            transaction.Descripcion = model.Descripcion.Trim();
            transaction.CategoriaId = model.CategoriaId;

            var balances = await _balanceService.CalculateAsync(
                transaction.Fecha,
                transaction.Id,
                transaction.Monto,
                categoria.Tipo);

            transaction.SaldoAnterior = balances.saldoAnterior;
            transaction.SaldoNuevo = balances.saldoNuevo;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists = await _context.Transaccion.AsNoTracking().AnyAsync(t => t.Id == id);
                if (!exists)
                {
                    return OperationResult.NotFound();
                }

                throw;
            }

            var recalculationStart = GetEarlierPosition(previousPosition, (transaction.Fecha, transaction.Id));
            await _balanceService.RecalculateFromAsync(recalculationStart.Fecha, recalculationStart.Id);

            _logger.LogInformation("Transaction {TransactionId} updated.", transaction.Id);
            return OperationResult.Success();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var transaction = await _context.Transaccion.FindAsync(id);
            if (transaction is null)
            {
                return false;
            }

            var removedPosition = (transaction.Fecha, transaction.Id);

            _context.Transaccion.Remove(transaction);
            await _context.SaveChangesAsync();
            await _balanceService.RecalculateFromAsync(removedPosition.Fecha, removedPosition.Id);

            _logger.LogInformation("Transaction {TransactionId} deleted.", id);
            return true;
        }

        public Task RecalculateAllAsync()
        {
            _logger.LogInformation("Full balance recalculation requested.");
            return _balanceService.RecalculateFromAsync(DateTime.MinValue, null);
        }

        private static DateTime ParseDateOrToday(string? rawDate)
        {
            return DateTime.TryParseExact(
                rawDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var selectedDate)
                ? selectedDate
                : DateTime.Today;
        }

        private static (DateTime Fecha, int Id) GetEarlierPosition(
            (DateTime Fecha, int Id) first,
            (DateTime Fecha, int Id) second)
        {
            if (first.Fecha < second.Fecha)
            {
                return first;
            }

            if (first.Fecha > second.Fecha)
            {
                return second;
            }

            return first.Id <= second.Id ? first : second;
        }
    }
}
