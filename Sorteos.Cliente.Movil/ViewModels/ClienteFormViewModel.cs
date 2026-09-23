using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLite;
using CommunityToolkit.Mvvm.Messaging;
using Sorteos.Cliente.Movil.Helpers;
using Sorteos.Cliente.Movil.Messages;
using Sorteos.Cliente.Movil.Models;
using Sorteos.Cliente.Movil.Services;

namespace Sorteos.Cliente.Movil.ViewModels;

/// <summary>
/// ViewModel responsable del formulario de captura, validacion y edicion de la ficha de un cliente.
/// </summary>
/// <remarks>
/// Consideraciones de Usabilidad:
/// - Adaptacion de Contexto: Detecta automaticamente si se trata de un alta o una modificacion mediante el parametro
///   <see cref="IdCliente"/>, actualizando el titulo de la pantalla ("Nuevo Cliente" vs "Editar Cliente").
/// - Asistencia en Captura Telefonica: Formatea y restringe el campo <see cref="Telefono"/> a un maximo de 10 digitos
///   en tiempo real, garantizando compatibilidad directa con los enlaces de comunicacion via WhatsApp.
/// - Validaciones Amigables y Guiadas: Verifica nombre obligatorio, longitud telefonica y formato de correo
///   proporcionando ejemplos claros (ej. "usuario@dominio.com") y evitando jerga tecnica en los dialogos de advertencia.
/// - Gestion Agil de Numeros de Planta: Permite agregar y remover boletos favoritos del cliente con verificacion
///   inmediata de disponibilidad en base de datos para no asignar numeros previamente reservados por terceros.
/// - Sincronizacion Desacoplada: Al guardar, emite <see cref="ClienteGuardadoMessage"/> para actualizar la lista principal
///   sin requerir recargas globales ni afectar la posicion de scroll.
/// </remarks>
[QueryProperty(nameof(IdCliente), "idCliente")]
public partial class ClienteFormViewModel : BaseViewModel
{
    private readonly ILocalDatabaseService _databaseService;

    #region propiedades

    /// <summary>
    /// Obtiene o establece el identificador del cliente a editar (0 para nuevos registros).
    /// </summary>
    [ObservableProperty]
    public partial int IdCliente { get; set; }

    /// <summary>
    /// Obtiene o establece el nombre de pila del cliente (campo obligatorio).
    /// </summary>
    [ObservableProperty]
    public partial string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el primer apellido del cliente.
    /// </summary>
    [ObservableProperty]
    public partial string ApellidoPaterno { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el segundo apellido del cliente.
    /// </summary>
    [ObservableProperty]
    public partial string ApellidoMaterno { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el telefono movil de contacto a 10 digitos.
    /// </summary>
    /// <remarks>
    /// Usabilidad: Se sanitiza automaticamente para contener solo digitos.
    /// </remarks>
    [ObservableProperty]
    public partial string Telefono { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece la direccion de correo electronico (opcional).
    /// </summary>
    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el estado activo o suspendido del cliente en el padron.
    /// </summary>
    [ObservableProperty]
    public partial bool Activo { get; set; } = true;

    /// <summary>
    /// Obtiene o establece la coleccion de numeros de boleto preferidos (numeros de planta) asignados al cliente.
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<int> NumerosPlanta { get; set; } = [];

    /// <summary>
    /// Obtiene o establece el texto del campo de captura rapida para anadir un nuevo numero de planta.
    /// </summary>
    [ObservableProperty]
    public partial string NuevoNumeroPlantaTexto { get; set; } = string.Empty;

    #endregion

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ClienteFormViewModel"/> configurando el titulo inicial.
    /// </summary>
    /// <param name="databaseService">Servicio de datos local.</param>
    public ClienteFormViewModel(ILocalDatabaseService databaseService)
    {
        _databaseService = databaseService;
        Title = "Nuevo Cliente";
    }

    /// <summary>
    /// Reacciona a la recepcion del parametro de consulta para cargar la ficha del cliente en modo edicion.
    /// </summary>
    async partial void OnIdClienteChanged(int value)
    {
        if (value > 0)
        {
            Title = "Editar Cliente";
            await CargarClienteAsync(value);
        }
        else
        {
            Title = "Nuevo Cliente";
        }
    }

    /// <summary>
    /// Sanitiza en tiempo real la entrada de texto telefonico para conservar unicamente hasta 10 digitos numericos.
    /// </summary>
    partial void OnTelefonoChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        string soloNumeros = value.SoloDigitos(10);
        if (soloNumeros != value)
        {
            Telefono = soloNumeros;
        }
    }

    /// <summary>
    /// Recupera la informacion del cliente y sus numeros de planta desde el almacenamiento local.
    /// </summary>
    /// <param name="idCliente">Identificador unico del cliente.</param>
    private async Task CargarClienteAsync(int idCliente)
    {
        try
        {
            IsBusy = true;
            Clientes? cliente = await _databaseService.ObtenerClientePorIdAsync(idCliente);
            if (cliente != null)
            {
                Nombre = cliente.Nombre;
                ApellidoPaterno = cliente.ApellidoPaterno;
                ApellidoMaterno = cliente.ApellidoMaterno;
                Telefono = cliente.Telefono;
                Email = cliente.Email;
                Activo = cliente.Activo;

                List<NumeroPlanta> plantas = await _databaseService.ObtenerNumerosPlantaPorClienteAsync(idCliente);
                NumerosPlanta = new ObservableCollection<int>(plantas.Select(p => p.Numero));
            }
        }
        catch (SQLiteException)
        {
            await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible cargar la información del cliente debido a un problema de lectura en el almacenamiento local.", "Aceptar");
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un inconveniente inesperado al abrir la ficha del cliente.", "Aceptar");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Agrega un nuevo numero de planta a la lista en memoria tras validar formato y disponibilidad global.
    /// </summary>
    /// <remarks>
    /// Usabilidad: Previene conflictos de asignacion notificando de inmediato si el boleto ya pertenece
    /// a otro cliente registrado en el sistema.
    /// </remarks>
    [RelayCommand]
    private async Task AgregarNumeroPlantaAsync()
    {
        if (string.IsNullOrWhiteSpace(NuevoNumeroPlantaTexto)) return;

        if (!int.TryParse(NuevoNumeroPlantaTexto.Trim(), out int numero) || numero < 0)
        {
            await Shell.Current.DisplayAlertAsync("Validación", "Ingresa un número entero válido mayor o igual a 0.", "Aceptar");
            return;
        }

        if (NumerosPlanta.Contains(numero))
        {
            await Shell.Current.DisplayAlertAsync("Aviso", "Este cliente ya tiene asignado este número.", "Aceptar");
            return;
        }

        // Validar unicidad en la base de datos contra otros clientes
        bool yaExiste = await _databaseService.ExisteNumeroPlantaAsync(numero, IdCliente);
        if (yaExiste)
        {
            await Shell.Current.DisplayAlertAsync("No disponible", $"El número {numero:D2} ya está apartado como número de planta por otro cliente.", "Aceptar");
            return;
        }

        NumerosPlanta.Add(numero);
        NuevoNumeroPlantaTexto = string.Empty;
    }

    /// <summary>
    /// Remueve un numero de planta de la lista del cliente en edicion.
    /// </summary>
    /// <param name="numero">Numero a retirar de la lista.</param>
    [RelayCommand]
    private void QuitarNumeroPlanta(int numero)
    {
        NumerosPlanta.Remove(numero);
    }

    /// <summary>
    /// Valida exhaustivamente cada campo del formulario y persiste la entidad del cliente y sus numeros de planta.
    /// </summary>
    /// <remarks>
    /// Usabilidad: Sanitiza cadenas, emite mensajes de error precisos y amigables ante fallos de formato,
    /// y notifica via mensajeria para que la lista receptora mantenga la continuidad de uso.
    /// </remarks>
    [RelayCommand]
    private async Task GuardarAsync()
    {
        // 1. Validacion y limpieza de Nombre
        string nombreIngresado = Nombre ?? string.Empty;
        string nombreLimpio = nombreIngresado.Clean().Trim();

        if (string.IsNullOrWhiteSpace(nombreLimpio))
        {
            await Shell.Current.DisplayAlertAsync("Validación", "El nombre es obligatorio.", "Aceptar");
            return;
        }

        if (nombreIngresado.ContieneCaracteresEspeciales())
        {
            await Shell.Current.DisplayAlertAsync("Validación", "El nombre no debe contener caracteres especiales ni números.", "Aceptar");
            return;
        }

        // 2. Validacion y limpieza de Apellidos (opcionales)
        string apellidoPaternoIngresado = ApellidoPaterno ?? string.Empty;
        string apellidoPaternoLimpio = apellidoPaternoIngresado.CleanAllWhiteSpace();
        if (!string.IsNullOrWhiteSpace(apellidoPaternoLimpio))
        {
            if (apellidoPaternoIngresado.ContieneCaracteresEspeciales())
            {
                await Shell.Current.DisplayAlertAsync("Validación", "El apellido paterno no debe contener caracteres especiales ni números.", "Aceptar");
                return;
            }
            
        }

        string apellidoMaternoIngresado = ApellidoMaterno ?? string.Empty;
        string apellidoMaternoLimpio = apellidoMaternoIngresado.CleanAllWhiteSpace();
        if (!string.IsNullOrWhiteSpace(apellidoMaternoLimpio))
        {
            if (apellidoMaternoIngresado.ContieneCaracteresEspeciales())
            {
                await Shell.Current.DisplayAlertAsync("Validación", "El apellido materno no debe contener caracteres especiales ni números.", "Aceptar");
                return;
            }

            
        }

        // 3. Validacion de Telefono
        string telefonoLimpio = Telefono.Clean();
        if (string.IsNullOrWhiteSpace(telefonoLimpio))
        {
            await Shell.Current.DisplayAlertAsync("Validación", "El teléfono de contacto es obligatorio.", "Aceptar");
            return;
        }

        if (telefonoLimpio.Length != 10 || !telefonoLimpio.EsSoloNumeros())
        {
            await Shell.Current.DisplayAlertAsync("Validación", "El teléfono debe contener exactamente 10 dígitos numéricos.", "Aceptar");
            return;
        }

        // 4. Validacion de Correo Electronico (opcional)
        string emailLimpio = Email.Clean();
        if (!string.IsNullOrWhiteSpace(emailLimpio))
        {
            if (!TextoHelper.EsEmailValido(emailLimpio))
            {
                await Shell.Current.DisplayAlertAsync("Validación", "El formato del correo electrónico no es válido. Ejemplo: usuario@dominio.com", "Aceptar");
                return;
            }
        }

        try
        {
            IsBusy = true;
            string resumenNumeros = NumerosPlanta.Count > 0
                ? string.Join(", ", NumerosPlanta.OrderBy(n => n).Select(n => n < 10 ? $"0{n}" : $"{n}"))
                : string.Empty;

            Clientes cliente = new()
            {
                Id = IdCliente,
                Nombre = nombreLimpio,
                ApellidoPaterno = apellidoPaternoLimpio,
                ApellidoMaterno = apellidoMaternoLimpio,
                Telefono = telefonoLimpio,
                Email = emailLimpio,
                Activo = Activo,
                NumerosPlantaResumen = resumenNumeros
            };

            bool esNuevo = IdCliente <= 0;
            int idGuardado = await _databaseService.GuardarClienteAsync(cliente);
            int clienteId = IdCliente > 0 ? IdCliente : idGuardado;
            cliente.Id = clienteId;

            // Guardar numeros de planta
            await _databaseService.GuardarNumerosPlantaClienteAsync(clienteId, [.. NumerosPlanta]);

            WeakReferenceMessenger.Default.Send(new ClienteGuardadoMessage(cliente, esNuevo));

            await Shell.Current.GoToAsync("..");
        }
        catch (SQLiteException)
        {
            await Shell.Current.DisplayAlertAsync("Aviso", "No fue posible guardar la información del cliente en el almacenamiento local. Por favor, verifica que los datos ingresados no generen conflictos e inténtalo nuevamente.", "Aceptar");
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Aviso", "Ocurrió un inconveniente inesperado al procesar el guardado del cliente. Por favor, inténtalo nuevamente.", "Aceptar");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Comando para descartar los cambios y retornar a la vista anterior.
    /// </summary>
    [RelayCommand]
    private static async Task CancelarAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    
}

