using ControlFinancieroProject.Models;

namespace ControlFinancieroProject.Services
{
    public interface ITransactionBalanceService
    {
        Task<(decimal saldoAnterior, decimal saldoNuevo)> CalculateAsync(
            DateTime fecha,
            int? transactionId,
            decimal monto,
            TipoCategoria tipoCategoria);

        Task RecalculateFromAsync(DateTime fromDate, int? fromId);
    }
}
