using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ControlFinancieroProject.Models
{
    public class Transaccion
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "La fecha es obligatoria.")]
        [Display(Name = "Fecha")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime Fecha { get; set; }

        [Required(ErrorMessage = "El monto es obligatorio.")]
        [Display(Name = "Monto")]
        [DataType(DataType.Currency)]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor que 0.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        [Display(Name = "Descripción")]
        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres.")]
        public string Descripcion { get; set; } = string.Empty;

        [Display(Name = "Saldo Anterior")]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        [BindNever]
        [ScaffoldColumn(false)]
        public decimal SaldoAnterior { get; set; }

        [Display(Name = "Saldo Nuevo")]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        [BindNever]
        [ScaffoldColumn(false)]
        public decimal SaldoNuevo { get; set; }

        [Required(ErrorMessage = "La categoría es obligatoria.")]
        [Display(Name = "Categoría")]
        public int CategoriaId { get; set; }

        [ForeignKey(nameof(CategoriaId))]
        [ValidateNever] // evita que el validador marque la navegación como campo requerido
        [BindNever]     // opcional: evita binding accidental de la entidad desde el formulario
        public Categoria? Categoria { get; set; }
    }
}
