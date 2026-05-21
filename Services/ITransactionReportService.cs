using ControlFinancieroProject.Models;

namespace ControlFinancieroProject.Services
{
    public interface ITransactionReportService
    {
        Task<MonthlyReportViewModel> GetMonthlyReportAsync(int? year, int? month);

        Task<ComparativeReportViewModel> GetComparativeReportAsync(int? year1, int? month1, int? year2, int? month2);
    }
}
