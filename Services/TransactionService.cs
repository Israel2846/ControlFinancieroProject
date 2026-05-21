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

        public async Task<TransactionIndexViewModel> GetIndexViewModelAsync(
            DateTime? fromDate,
            DateTime? toDate,
            string? searchTerm,
            TipoCategoria? tipo,
            int[]? categoryIds)
        {
            var selectedCategoryIds = NormalizeCategoryIds(categoryIds);
            var normalizedSearch = NormalizeSearchTerm(searchTerm);
            var hasNonDateFilters = !string.IsNullOrWhiteSpace(normalizedSearch) || tipo.HasValue || selectedCategoryIds.Count > 0;

            if (!fromDate.HasValue && !toDate.HasValue && !hasNonDateFilters)
            {
                fromDate = DateTime.Today;
                toDate = DateTime.Today;
            }

            var model = new TransactionIndexViewModel
            {
                SearchTerm = normalizedSearch,
                FromDate = fromDate?.Date,
                ToDate = toDate?.Date,
                SelectedTipo = tipo,
                SelectedCategoryIds = selectedCategoryIds,
                Categorias = await GetCategoryOptionsAsync()
            };

            if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
            {
                model.FilterErrorMessage = "La fecha inicial no puede ser mayor que la fecha final.";
                return model;
            }

            var query = _context.Transaccion
                .AsNoTracking()
                .Include(t => t.Categoria)
                .AsQueryable();

            if (model.FromDate.HasValue)
            {
                query = query.Where(t => t.Fecha >= model.FromDate.Value);
            }

            if (model.ToDate.HasValue)
            {
                var endExclusive = model.ToDate.Value.AddDays(1);
                query = query.Where(t => t.Fecha < endExclusive);
            }

            if (!string.IsNullOrWhiteSpace(model.SearchTerm))
            {
                query = query.Where(t =>
                    EF.Functions.Like(t.Descripcion, $"%{model.SearchTerm}%") ||
                    (t.Categoria != null && EF.Functions.Like(t.Categoria.Descripcion, $"%{model.SearchTerm}%")));
            }

            if (model.SelectedTipo.HasValue)
            {
                query = query.Where(t => t.Categoria != null && t.Categoria.Tipo == model.SelectedTipo.Value);
            }

            if (model.SelectedCategoryIds.Count > 0)
            {
                query = query.Where(t => model.SelectedCategoryIds.Contains(t.CategoriaId));
            }

            var transacciones = await query
                .OrderByDescending(t => t.Fecha)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            model.Transacciones = transacciones;
            model.FilteredIncome = transacciones
                .Where(t => t.Categoria?.Tipo == TipoCategoria.Ingreso)
                .Sum(t => t.Monto);
            model.FilteredExpense = transacciones
                .Where(t => t.Categoria?.Tipo == TipoCategoria.Gasto)
                .Sum(t => t.Monto);

            return model;
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
            var categorias = await GetCategoryOptionsAsync();
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

        private async Task<IReadOnlyList<CategoryOptionViewModel>> GetCategoryOptionsAsync()
        {
            return await _context.Categoria
                .AsNoTracking()
                .OrderBy(c => c.Tipo)
                .ThenBy(c => c.Descripcion)
                .Select(c => new CategoryOptionViewModel
                {
                    Id = c.Id,
                    Descripcion = c.Descripcion,
                    Tipo = c.Tipo
                })
                .ToListAsync();
        }

        private static string? NormalizeSearchTerm(string? searchTerm)
        {
            return string.IsNullOrWhiteSpace(searchTerm)
                ? null
                : searchTerm.Trim();
        }

        private static IReadOnlyList<int> NormalizeCategoryIds(int[]? categoryIds)
        {
            return categoryIds?
                .Where(id => id > 0)
                .Distinct()
                .OrderBy(id => id)
                .ToArray() ?? Array.Empty<int>();
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
