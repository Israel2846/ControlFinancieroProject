using ControlFinancieroProject.Data;
using ControlFinancieroProject.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlFinancieroProject.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;

        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Categoria>> GetAllAsync()
        {
            return await _context.Categoria
                .AsNoTracking()
                .OrderBy(c => c.Descripcion)
                .ToListAsync();
        }

        public async Task<Categoria?> GetByIdAsync(int id, bool includeTransactions = false)
        {
            IQueryable<Categoria> query = _context.Categoria.AsNoTracking();

            if (includeTransactions)
            {
                query = query.Include(c => c.Transacciones);
            }

            return await query.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task CreateAsync(Categoria categoria)
        {
            _context.Categoria.Add(categoria);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateAsync(Categoria categoria)
        {
            _context.Categoria.Update(categoria);

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists = await _context.Categoria.AnyAsync(c => c.Id == categoria.Id);
                if (!exists)
                {
                    return false;
                }

                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var categoria = await _context.Categoria.FindAsync(id);
            if (categoria is null)
            {
                return false;
            }

            _context.Categoria.Remove(categoria);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
