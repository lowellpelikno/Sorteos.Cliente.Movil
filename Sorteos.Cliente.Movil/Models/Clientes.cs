using SQLite;

namespace Sorteos.Cliente.Movil.Models
{
    [Table("Clientes")]
    public class Clientes
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string ApellidoPaterno { get; set; } = string.Empty;

        public string ApellidoMaterno { get; set; } = string.Empty;

        [Indexed]
        public string Telefono { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        [Ignore]
        public string NombreCompleto
        {
            get
            {
                IEnumerable<string> partes = new[] { Nombre, ApellidoPaterno, ApellidoMaterno }
                    .Where(p => !string.IsNullOrWhiteSpace(p));
                return string.Join(" ", partes);
            }
        }

        public string NumerosPlantaResumen { get; set; } = string.Empty;

        [Ignore]
        public bool TieneNumerosPlanta => !string.IsNullOrWhiteSpace(NumerosPlantaResumen);
    }
}

