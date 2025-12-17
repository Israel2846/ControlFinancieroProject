using System.Collections.Generic;

namespace ControlFinancieroProject.Models
{
    public class ComparativeReportViewModel
    {
        public int Year1 { get; set; }
        public int Month1 { get; set; }

        public int Year2 { get; set; }
        public int Month2 { get; set; }

        // General (fallback)
        public List<string> CategoryLabels { get; set; } = new List<string>();
        public List<decimal> ValuesMonth1 { get; set; } = new List<decimal>();
        public List<decimal> ValuesMonth2 { get; set; } = new List<decimal>();

        // Específicos: gastos
        public List<string> ExpenseCategoryLabels { get; set; } = new List<string>();
        public List<decimal> ExpenseValuesMonth1 { get; set; } = new List<decimal>();
        public List<decimal> ExpenseValuesMonth2 { get; set; } = new List<decimal>();

        // Específicos: ingresos
        public List<string> IncomeCategoryLabels { get; set; } = new List<string>();
        public List<decimal> IncomeValuesMonth1 { get; set; } = new List<decimal>();
        public List<decimal> IncomeValuesMonth2 { get; set; } = new List<decimal>();

        public decimal TotalMonth1 { get; set; }
        public decimal TotalMonth2 { get; set; }

        public IEnumerable<Transaccion> TransaccionesMonth1 { get; set; } = new List<Transaccion>();
        public IEnumerable<Transaccion> TransaccionesMonth2 { get; set; } = new List<Transaccion>();
    }
}