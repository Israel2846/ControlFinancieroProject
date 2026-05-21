using ControlFinancieroProject.Models;

namespace ControlFinancieroProject.Services
{
    public interface ICategoryService
    {
        Task<IReadOnlyList<Categoria>> GetAllAsync();

        Task<Categoria?> GetByIdAsync(int id, bool includeTransactions = false);

        Task CreateAsync(Categoria categoria);

        Task<bool> UpdateAsync(Categoria categoria);

        Task<bool> DeleteAsync(int id);
    }
}
