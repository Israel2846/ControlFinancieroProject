namespace ControlFinancieroProject.Models
{
    public class MonthlyReportViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }

        public IReadOnlyList<Transaccion> Transacciones { get; set; } = Array.Empty<Transaccion>();

        public List<string> DailyLabels { get; set; } = new List<string>();
        public List<decimal> DailyExpenses { get; set; } = new List<decimal>();

        public List<string> ExpenseCategoryLabels { get; set; } = new List<string>();
        public List<decimal> ExpenseCategoryValues { get; set; } = new List<decimal>();

        public List<string> IncomeCategoryLabels { get; set; } = new List<string>();
        public List<decimal> IncomeCategoryValues { get; set; } = new List<decimal>();

        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }

        public decimal CurrentBalance { get; set; }
    }
}