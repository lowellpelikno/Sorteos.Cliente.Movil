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

[QueryProperty(nameof(IdCliente), "idCliente")]
public partial class ClienteFormViewModel : BaseViewModel
{
    private readonly ILocalDatabaseService _databaseService;

    #region propiedades

    

    [ObservableProperty]
    public partial int IdCliente { get; set; }

    [ObservableProperty]
    public partial string Nombre { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ApellidoPaterno { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ApellidoMaterno { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Telefono { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool Activo { get; set; } = true;

    [ObservableProperty]
    public partial ObservableCollection<int> NumerosPlanta { get; set; } = [];

    [ObservableProperty]
    public partial string NuevoNumeroPlantaTexto { get; set; } = string.Empty;

    #endregion

    public ClienteFormViewModel(ILocalDatabaseService databaseService)
    {
        _databaseService = databaseService;
        Title = "Nuevo Cliente";
    }

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

    partial void OnTelefonoChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        string soloNumeros = value.SoloDigitos(10);
        if (soloNumeros != value)
        {
            Telefono = soloNumeros;
        }
    }

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

    [RelayCommand]
    private void QuitarNumeroPlanta(int numero)
    {
        NumerosPlanta.Remove(numero);
    }

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

    [RelayCommand]
    private static async Task CancelarAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    
}

