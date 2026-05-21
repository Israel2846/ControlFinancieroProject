namespace ControlFinancieroProject.Models
{
    public class TransactionIndexViewModel
    {
        public string? SearchTerm { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public TipoCategoria? SelectedTipo { get; set; }

        public IReadOnlyList<int> SelectedCategoryIds { get; set; } = Array.Empty<int>();

        public IReadOnlyList<CategoryOptionViewModel> Categorias { get; set; } = Array.Empty<CategoryOptionViewModel>();

        public string? FilterErrorMessage { get; set; }

        public IReadOnlyList<Transaccion> Transacciones { get; set; } = Array.Empty<Transaccion>();

        public decimal FilteredIncome { get; set; }

        public decimal FilteredExpense { get; set; }

        public decimal FilteredNet => FilteredIncome - FilteredExpense;

        public int TransactionCount => Transacciones.Count;

        public bool HasDateRange => FromDate.HasValue || ToDate.HasValue;

        public bool IsSingleDayFilter =>
            FromDate.HasValue &&
            ToDate.HasValue &&
            FromDate.Value.Date == ToDate.Value.Date;

        public bool HasAdvancedFilters =>
            !string.IsNullOrWhiteSpace(SearchTerm) ||
            SelectedTipo.HasValue ||
            SelectedCategoryIds.Count > 0 ||
            (HasDateRange && !IsSingleDayFilter);
    }
}
