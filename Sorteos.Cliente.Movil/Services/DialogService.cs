namespace Sorteos.Cliente.Movil.Services
{
    /// <summary>
    /// Implementacion del servicio de dialogos y notificaciones visuales delegada a Shell.Current.
    /// </summary>
    public class DialogService : IDialogService
    {
        /// <inheritdoc/>
        public async Task DisplayAlertAsync(string title, string message, string cancel)
        {
            if (Shell.Current != null)
            {
                await Shell.Current.DisplayAlertAsync(title, message, cancel);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel)
        {
            if (Shell.Current != null)
            {
                return await Shell.Current.DisplayAlertAsync(title, message, accept, cancel);
            }
            return false;
        }
    }
}

