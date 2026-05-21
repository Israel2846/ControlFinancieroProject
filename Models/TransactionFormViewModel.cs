using System.ComponentModel.DataAnnotations;

namespace ControlFinancieroProject.Models
{
    public class TransactionFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La fecha es obligatoria.")]
        [Display(Name = "Fecha")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime Fecha { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "El monto es obligatorio.")]
        [Display(Name = "Monto")]
        [DataType(DataType.Currency)]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor que 0.")]
        public decimal Monto { get; set; }

        [Display(Name = "Descripción")]
        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres.")]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "La categoría es obligatoria.")]
        [Display(Name = "Categoría")]
        public int CategoriaId { get; set; }

        [Display(Name = "Tipo")]
        public TipoCategoria? SelectedTipo { get; set; }

        public IReadOnlyList<CategoryOptionViewModel> Categorias { get; set; } = Array.Empty<CategoryOptionViewModel>();
    }
}
