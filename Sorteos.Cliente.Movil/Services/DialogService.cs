namespace Sorteos.Cliente.Movil.Services
{
    public class DialogService : IDialogService
    {
        public async Task DisplayAlertAsync(string title, string message, string cancel)
        {
            if (Shell.Current != null)
            {
                await Shell.Current.DisplayAlertAsync(title, message, cancel);
            }
        }

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

