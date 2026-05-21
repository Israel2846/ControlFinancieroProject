using ControlFinancieroProject.Data;
using ControlFinancieroProject.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlFinancieroProject.Services
{
    public class TransactionBalanceService : ITransactionBalanceService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TransactionBalanceService> _logger;

        public TransactionBalanceService(
            ApplicationDbContext context,
            ILogger<TransactionBalanceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(decimal saldoAnterior, decimal saldoNuevo)> CalculateAsync(
            DateTime fecha,
            int? transactionId,
            decimal monto,
            TipoCategoria tipoCategoria)
        {
            var previousQuery = _context.Transaccion
                .AsNoTracking()
                .AsQueryable();

            if (transactionId.HasValue)
            {
                previousQuery = previousQuery
                    .Where(t => t.Id != transactionId.Value)
                    .Where(t => t.Fecha < fecha || (t.Fecha == fecha && t.Id < transactionId.Value));
            }
            else
            {
                previousQuery = previousQuery.Where(t => t.Fecha <= fecha);
            }

            var previous = await previousQuery
                .OrderByDescending(t => t.Fecha)
                .ThenByDescending(t => t.Id)
                .FirstOrDefaultAsync();

            var saldoAnterior = previous?.SaldoNuevo ?? 0m;
            var saldoNuevo = tipoCategoria == TipoCategoria.Ingreso
                ? saldoAnterior + monto
                : saldoAnterior - monto;

            return (saldoAnterior, saldoNuevo);
        }

        public async Task RecalculateFromAsync(DateTime fromDate, int? fromId)
        {
            _logger.LogInformation("Recalculating balances from {FromDate} / {FromId}", fromDate, fromId);

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
                previousQuery = previousQuery.Where(_ => false);
            }

            var previous = await previousQuery
                .OrderByDescending(t => t.Fecha)
                .ThenByDescending(t => t.Id)
                .FirstOrDefaultAsync();

            var running = previous?.SaldoNuevo ?? 0m;
            var previousDate = previous?.Fecha ?? DateTime.MinValue;
            var previousId = previous?.Id ?? int.MinValue;

            var affectedQuery = _context.Transaccion
                .Include(t => t.Categoria)
                .AsQueryable();

            if (fromDate > DateTime.MinValue || fromId.HasValue)
            {
                affectedQuery = affectedQuery.Where(t => t.Fecha > previousDate || (t.Fecha == previousDate && t.Id > previousId));
            }

            var affected = await affectedQuery
                .OrderBy(t => t.Fecha)
                .ThenBy(t => t.Id)
                .ToListAsync();

            if (affected.Count == 0)
            {
                return;
            }

            foreach (var transaction in affected)
            {
                transaction.SaldoAnterior = running;
                running = transaction.Categoria?.Tipo == TipoCategoria.Ingreso
                    ? running + transaction.Monto
                    : running - transaction.Monto;
                transaction.SaldoNuevo = running;
            }

            await _context.SaveChangesAsync();
        }
    }
}
