using Sorteos.Cliente.Movil.ViewModels;

namespace Sorteos.Cliente.Movil.Views
{
/// <summary>
    /// Pagina de agenda semanal que permite navegar interactivamente entre los dias de la semana,
    /// consultar los sorteos programados, generar nuevos sorteos y exportar el resumen de premiacion.
    /// </summary>
    public partial class SorteosSemanaPage : ContentPage
    {
        private readonly SorteosSemanaViewModel _viewModel;
        private bool _yaInicializado;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="SorteosSemanaPage"/> vinculando su ViewModel y configurando los controladores de desplazamiento y captura.
        /// </summary>
        /// <param name="viewModel">Instancia inyectada de <see cref="SorteosSemanaViewModel"/>.</param>
        public SorteosSemanaPage(SorteosSemanaViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
            _viewModel.SolicitudDesplazarADia += OnSolicitudDesplazarADia;
            _viewModel.SolicitarCapturaFlyerGanadoresAsync = CapturarFlyerGanadoresAsync;
        }

        /// <summary>
        /// Captura como imagen el componente grafico de premiacion de ganadores para su exportacion a redes o mensajeria.
        /// </summary>
        /// <returns>Ruta local al archivo de imagen generado, o null en caso de error.</returns>
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

        /// <summary>
        /// Desplaza suavemente la barra de dias de la semana hasta centrar el dia seleccionado por el usuario.
        /// </summary>
        /// <param name="dia">Elemento de dia seleccionado.</param>
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

