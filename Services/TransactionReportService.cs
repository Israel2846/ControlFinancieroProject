using ControlFinancieroProject.Data;
using ControlFinancieroProject.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlFinancieroProject.Services
{
    public class TransactionReportService : ITransactionReportService
    {
        private readonly ApplicationDbContext _context;

        public TransactionReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<MonthlyReportViewModel> GetMonthlyReportAsync(int? year, int? month)
        {
            var now = DateTime.Now;
            var selectedYear = year ?? now.Year;
            var selectedMonth = month ?? now.Month;

            var periodStart = new DateTime(selectedYear, selectedMonth, 1);
            var periodEnd = periodStart.AddMonths(1);

            var transacciones = await _context.Transaccion
                .AsNoTracking()
                .Include(t => t.Categoria)
                .Where(t => t.Fecha >= periodStart && t.Fecha < periodEnd)
                .OrderByDescending(t => t.Fecha)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            var daysInMonth = DateTime.DaysInMonth(selectedYear, selectedMonth);
            var dailyLabels = new List<string>(daysInMonth);
            var dailyExpenses = new List<decimal>(daysInMonth);

            for (var day = 1; day <= daysInMonth; day++)
            {
                var currentDate = new DateTime(selectedYear, selectedMonth, day);
                dailyLabels.Add(currentDate.ToString("yyyy-MM-dd"));
                dailyExpenses.Add(transacciones
                    .Where(t => t.Categoria?.Tipo == TipoCategoria.Gasto && t.Fecha.Date == currentDate.Date)
                    .Sum(t => t.Monto));
            }

            var expenseGroups = transacciones
                .Where(t => t.Categoria?.Tipo == TipoCategoria.Gasto)
                .GroupBy(t => t.Categoria!.Descripcion)
                .Select(g => new { Label = g.Key, Value = g.Sum(t => t.Monto) })
                .OrderByDescending(g => g.Value)
                .ToList();

            var incomeGroups = transacciones
                .Where(t => t.Categoria?.Tipo == TipoCategoria.Ingreso)
                .GroupBy(t => t.Categoria!.Descripcion)
                .Select(g => new { Label = g.Key, Value = g.Sum(t => t.Monto) })
                .OrderByDescending(g => g.Value)
                .ToList();

            var currentBalance = await _context.Transaccion
                .AsNoTracking()
                .OrderByDescending(t => t.Fecha)
                .ThenByDescending(t => t.Id)
                .Select(t => t.SaldoNuevo)
                .FirstOrDefaultAsync();

            return new MonthlyReportViewModel
            {
                Year = selectedYear,
                Month = selectedMonth,
                Transacciones = transacciones,
                DailyLabels = dailyLabels,
                DailyExpenses = dailyExpenses,
                ExpenseCategoryLabels = expenseGroups.Select(g => g.Label).ToList(),
                ExpenseCategoryValues = expenseGroups.Select(g => g.Value).ToList(),
                IncomeCategoryLabels = incomeGroups.Select(g => g.Label).ToList(),
                IncomeCategoryValues = incomeGroups.Select(g => g.Value).ToList(),
                TotalExpense = expenseGroups.Sum(g => g.Value),
                TotalIncome = incomeGroups.Sum(g => g.Value),
                CurrentBalance = currentBalance
            };
        }

        public async Task<ComparativeReportViewModel> GetComparativeReportAsync(int? year1, int? month1, int? year2, int? month2)
        {
            var now = DateTime.Now;
            var selectedYear1 = year1 ?? now.Year;
            var selectedMonth1 = month1 ?? now.Month;
            var selectedYear2 = year2 ?? now.Year;
            var selectedMonth2 = month2 ?? now.Month;

            var firstPeriodStart = new DateTime(selectedYear1, selectedMonth1, 1);
            var secondPeriodStart = new DateTime(selectedYear2, selectedMonth2, 1);

            var firstPeriodTransactions = await GetTransactionsForMonthAsync(firstPeriodStart);
            var secondPeriodTransactions = await GetTransactionsForMonthAsync(secondPeriodStart);

            var expenseCategories = GetDistinctCategoryLabels(firstPeriodTransactions, secondPeriodTransactions, TipoCategoria.Gasto);
            var incomeCategories = GetDistinctCategoryLabels(firstPeriodTransactions, secondPeriodTransactions, TipoCategoria.Ingreso);

            var allCategories = await _context.Categoria
                .AsNoTracking()
                .OrderBy(c => c.Descripcion)
                .Select(c => c.Descripcion)
                .ToListAsync();

            return new ComparativeReportViewModel
            {
                Year1 = selectedYear1,
                Month1 = selectedMonth1,
                Year2 = selectedYear2,
                Month2 = selectedMonth2,
                CategoryLabels = allCategories,
                ValuesMonth1 = GetCategoryTotals(allCategories, firstPeriodTransactions),
                ValuesMonth2 = GetCategoryTotals(allCategories, secondPeriodTransactions),
                ExpenseCategoryLabels = expenseCategories,
                ExpenseValuesMonth1 = GetCategoryTotals(expenseCategories, firstPeriodTransactions),
                ExpenseValuesMonth2 = GetCategoryTotals(expenseCategories, secondPeriodTransactions),
                IncomeCategoryLabels = incomeCategories,
                IncomeValuesMonth1 = GetCategoryTotals(incomeCategories, firstPeriodTransactions),
                IncomeValuesMonth2 = GetCategoryTotals(incomeCategories, secondPeriodTransactions),
                TotalMonth1 = firstPeriodTransactions.Sum(t => t.Monto),
                TotalMonth2 = secondPeriodTransactions.Sum(t => t.Monto),
                TransaccionesMonth1 = firstPeriodTransactions
                    .OrderByDescending(t => t.Fecha)
                    .ThenByDescending(t => t.Id)
                    .ToList(),
                TransaccionesMonth2 = secondPeriodTransactions
                    .OrderByDescending(t => t.Fecha)
                    .ThenByDescending(t => t.Id)
                    .ToList()
            };
        }

        private async Task<List<Transaccion>> GetTransactionsForMonthAsync(DateTime periodStart)
        {
            var periodEnd = periodStart.AddMonths(1);

            return await _context.Transaccion
                .AsNoTracking()
                .Include(t => t.Categoria)
                .Where(t => t.Fecha >= periodStart && t.Fecha < periodEnd)
                .ToListAsync();
        }

        private static List<string> GetDistinctCategoryLabels(
            IEnumerable<Transaccion> firstPeriodTransactions,
            IEnumerable<Transaccion> secondPeriodTransactions,
            TipoCategoria tipoCategoria)
        {
            return firstPeriodTransactions
                .Concat(secondPeriodTransactions)
                .Where(t => t.Categoria?.Tipo == tipoCategoria)
                .Select(t => t.Categoria!.Descripcion)
                .Distinct()
                .OrderBy(label => label)
                .ToList();
        }

        private static List<decimal> GetCategoryTotals(
            IEnumerable<string> categories,
            IEnumerable<Transaccion> transactions)
        {
            return categories
                .Select(category => transactions
                    .Where(t => (t.Categoria?.Descripcion ?? string.Empty) == category)
                    .Sum(t => t.Monto))
                .ToList();
        }
    }
}
