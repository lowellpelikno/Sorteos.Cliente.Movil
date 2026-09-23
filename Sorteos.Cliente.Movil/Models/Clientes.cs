using SQLite;

namespace Sorteos.Cliente.Movil.Models
{
/// <summary>
    /// Entidad persistida en SQLite que representa un cliente o comprador registrado en el sistema.
    /// </summary>
    [Table("Clientes")]
    public class Clientes
    {
        /// <summary>
        /// Identificador primario y autoincrementable del cliente.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Nombre de pila del cliente.
        /// </summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Primer apellido del cliente.
        /// </summary>
        public string ApellidoPaterno { get; set; } = string.Empty;

        /// <summary>
        /// Segundo apellido del cliente.
        /// </summary>
        public string ApellidoMaterno { get; set; } = string.Empty;

        /// <summary>
        /// Numero telefonico de contacto, indexado para acelerar busquedas y filtros.
        /// </summary>
        [Indexed]
        public string Telefono { get; set; } = string.Empty;

        /// <summary>
        /// Correo electronico del cliente.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Indica si el cliente se encuentra activo para asignaciones y ventas.
        /// </summary>
        public bool Activo { get; set; } = true;

        /// <summary>
        /// Nombre completo consolidado, calculado en memoria a partir del nombre y apellidos.
        /// </summary>
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

        /// <summary>
        /// Resumen en texto de los numeros de planta asignados (poblado via GROUP_CONCAT en consultas SQL).
        /// </summary>
        public string NumerosPlantaResumen { get; set; } = string.Empty;

        /// <summary>
        /// Determina si el cliente cuenta con al menos un numero de planta registrado.
        /// </summary>
        [Ignore]
        public bool TieneNumerosPlanta => !string.IsNullOrWhiteSpace(NumerosPlantaResumen);
    }
}

