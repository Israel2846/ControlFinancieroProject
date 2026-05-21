namespace ControlFinancieroProject.Models
{
    public class CategoryOptionViewModel
    {
        public int Id { get; set; }

        public string Descripcion { get; set; } = string.Empty;

        public TipoCategoria Tipo { get; set; }
    }
}
