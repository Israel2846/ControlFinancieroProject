using ControlFinancieroProject.Models;

namespace ControlFinancieroProject.Services
{
    public interface ITransactionService
    {
        Task<TransactionIndexViewModel> GetIndexViewModelAsync(
            DateTime? fromDate,
            DateTime? toDate,
            string? searchTerm,
            TipoCategoria? tipo,
            int[]? categoryIds);

        Task<Transaccion?> GetByIdAsync(int id);

        Task<TransactionFormViewModel> GetCreateFormAsync();

        Task<TransactionFormViewModel?> GetEditFormAsync(int id);

        Task PopulateFormOptionsAsync(TransactionFormViewModel model);

        Task<OperationResult> CreateAsync(TransactionFormViewModel model);

        Task<OperationResult> UpdateAsync(int id, TransactionFormViewModel model);

        Task<bool> DeleteAsync(int id);

        Task RecalculateAllAsync();
    }
}
