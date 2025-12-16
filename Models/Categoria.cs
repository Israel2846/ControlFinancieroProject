using System.ComponentModel.DataAnnotations;

namespace ControlFinancieroProject.Models
{
    public enum TipoCategoria
    {
        Ingreso = 1,
        Gasto = 2
    }

    public class Categoria
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la categoría es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre de la categoría no puede exceder los 100 caracteres.")]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo de categoría es obligatorio.")]
        [EnumDataType(typeof(TipoCategoria), ErrorMessage = "El tipo de categoría no es válido.")]
        public TipoCategoria Tipo { get; set; } = TipoCategoria.Gasto;

        public ICollection<Transaccion> Transacciones { get; set; } = new List<Transaccion>();
    }
}
