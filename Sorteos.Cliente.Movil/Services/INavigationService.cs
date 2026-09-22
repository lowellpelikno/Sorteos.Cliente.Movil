namespace Sorteos.Cliente.Movil.Services
{
    public interface INavigationService
    {
        Task GoToAsync(string route);
        Task GoToAsync(string route, IDictionary<string, object> parameters);
        Task PopAsync();
    }
}

