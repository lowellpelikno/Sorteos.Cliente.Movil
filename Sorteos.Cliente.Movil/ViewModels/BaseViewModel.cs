using CommunityToolkit.Mvvm.ComponentModel;

namespace Sorteos.Cliente.Movil.ViewModels
{
    public partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        public partial bool IsBusy { get; set; }

        [ObservableProperty]
        public partial string Title { get; set; } = string.Empty;

        public bool IsNotBusy => !IsBusy;
    }
}

