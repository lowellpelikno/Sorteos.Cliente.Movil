namespace Sorteos.Cliente.Movil.Services
{
    /// <summary>
    /// Implementacion concreta del servicio de navegacion Shell delegada en Shell.Current.
    /// </summary>
    public class NavigationService : INavigationService
    {
        /// <inheritdoc/>
        public async Task GoToAsync(string route)
        {
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync(route);
            }
        }

        /// <inheritdoc/>
        public async Task GoToAsync(string route, IDictionary<string, object> parameters)
        {
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync(route, parameters);
            }
        }

        /// <inheritdoc/>
        public async Task PopAsync()
        {
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("..");
            }
        }
    }
}

