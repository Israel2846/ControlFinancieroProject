namespace ControlFinancieroProject.Models
{
    public class TransactionIndexViewModel
    {
        public DateTime SelectedDate { get; set; } = DateTime.Today;

        public string? SearchTerm { get; set; }

        public IReadOnlyList<Transaccion> Transacciones { get; set; } = Array.Empty<Transaccion>();

        public decimal DailyIncome { get; set; }

        public decimal DailyExpense { get; set; }

        public decimal DailyNet => DailyIncome - DailyExpense;

        public int TransactionCount => Transacciones.Count;
    }
}
