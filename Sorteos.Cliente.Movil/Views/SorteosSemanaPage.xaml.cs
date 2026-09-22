using Sorteos.Cliente.Movil.ViewModels;

namespace Sorteos.Cliente.Movil.Views
{
    public partial class SorteosSemanaPage : ContentPage
    {
        private readonly SorteosSemanaViewModel _viewModel;
        private bool _yaInicializado;

        public SorteosSemanaPage(SorteosSemanaViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
            _viewModel.SolicitudDesplazarADia += OnSolicitudDesplazarADia;
            _viewModel.SolicitarCapturaFlyerGanadoresAsync = CapturarFlyerGanadoresAsync;
        }

        private async Task<string?> CapturarFlyerGanadoresAsync()
        {
            try
            {
                if (tarjetaFlyerGanadores == null) return null;
                IScreenshotResult? screenshot = await tarjetaFlyerGanadores.CaptureAsync();
                if (screenshot == null) return null;

                string carpetaTemporal = Path.Combine(FileSystem.CacheDirectory, "export_ganadores");
                if (!Directory.Exists(carpetaTemporal))
                {
                    Directory.CreateDirectory(carpetaTemporal);
                }

                string archivoDestino = Path.Combine(carpetaTemporal, $"ganadores_sorteo_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                using (Stream readStream = await screenshot.OpenReadAsync())
                using (FileStream writeStream = File.Create(archivoDestino))
                {
                    await readStream.CopyToAsync(writeStream);
                }

                return archivoDestino;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void OnSolicitudDesplazarADia(Models.DiaSemanaItem dia)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Yield();
                diasCollectionView.ScrollTo(dia, position: ScrollToPosition.Center, animate: true);
            });
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            _viewModel.SolicitudDesplazarADia -= OnSolicitudDesplazarADia;
            _viewModel.SolicitudDesplazarADia += OnSolicitudDesplazarADia;
            _viewModel.SolicitarCapturaFlyerGanadoresAsync = CapturarFlyerGanadoresAsync;

            try
            {
                if (!_yaInicializado)
                {
                    _yaInicializado = true;
                    await _viewModel.InicializarSemanaAsync();
                }
            }
            catch (Exception)
            {
                // Salvaguarda defensiva para impedir excepciones no controladas en el hilo UI
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.SolicitudDesplazarADia -= OnSolicitudDesplazarADia;
            _viewModel.SolicitarCapturaFlyerGanadoresAsync = null;
        }
    }
}

