namespace Sorteos.Cliente.Movil.Services
{
    public class NavigationService : INavigationService
    {
        public async Task GoToAsync(string route)
        {
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync(route);
            }
        }

        public async Task GoToAsync(string route, IDictionary<string, object> parameters)
        {
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync(route, parameters);
            }
        }

        public async Task PopAsync()
        {
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("..");
            }
        }
    }
}

