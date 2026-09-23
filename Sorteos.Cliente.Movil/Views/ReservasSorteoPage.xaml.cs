using Sorteos.Cliente.Movil.ViewModels;

namespace Sorteos.Cliente.Movil.Views
{
/// <summary>
    /// Pagina principal de administracion y operacion de un sorteo, incluyendo la cuadricula de numeros,
    /// gestion de apartados, captura y exportacion de flyers publicitarios y registro de pagos masivos.
    /// </summary>
    public partial class ReservasSorteoPage : ContentPage
    {
        /// <summary>
        /// Inicializa una nueva instancia de <see cref="ReservasSorteoPage"/> vinculando su ViewModel y configurando el delegado de captura de flyer.
        /// </summary>
        /// <param name="viewModel">Instancia inyectada de <see cref="ReservasSorteoViewModel"/>.</param>
        public ReservasSorteoPage(ReservasSorteoViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            viewModel.SolicitarCapturaFlyerAsync = CapturarFlyerAsync;
        }

        /// <summary>
        /// Captura como imagen grafica el componente visual de flyer del sorteo y lo almacena temporalmente en disco para compartir.
        /// </summary>
        /// <returns>Ruta absoluta al archivo de imagen PNG generado, o null si la captura falla.</returns>
        private async Task<string?> CapturarFlyerAsync()
        {
            try
            {
                if (tarjetaFlyer == null) return null;
                IScreenshotResult? screenshot = await tarjetaFlyer.CaptureAsync();
                if (screenshot == null) return null;

                string carpetaTemporal = Path.Combine(FileSystem.CacheDirectory, "export_flyers");
                if (!Directory.Exists(carpetaTemporal))
                {
                    Directory.CreateDirectory(carpetaTemporal);
                }

                string archivoDestino = Path.Combine(carpetaTemporal, $"flyer_sorteo_{DateTime.Now:yyyyMMdd_HHmmss}.png");
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

#if WINDOWS
        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            ConfigurarScrollHorizontalWindows();
        }

        private void ConfigurarScrollHorizontalWindows()
        {
            if (scrollPildoras == null) return;

            if (scrollPildoras.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ScrollViewer nativeScrollViewer)
            {
                nativeScrollViewer.PointerWheelChanged -= OnNativePointerWheelChanged;
                nativeScrollViewer.PointerWheelChanged += OnNativePointerWheelChanged;
            }
            else
            {
                scrollPildoras.HandlerChanged += (s, e) =>
                {
                    if (scrollPildoras.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ScrollViewer sv)
                    {
                        sv.PointerWheelChanged -= OnNativePointerWheelChanged;
                        sv.PointerWheelChanged += OnNativePointerWheelChanged;
                    }
                };
            }
        }

        private void OnNativePointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            int delta = e.GetCurrentPoint(null).Properties.MouseWheelDelta;
            if (delta != 0 && scrollPildoras != null)
            {
                double targetX = Math.Clamp(scrollPildoras.ScrollX - delta, 0, Math.Max(0, scrollPildoras.ContentSize.Width - scrollPildoras.Width));
                scrollPildoras.ScrollToAsync(targetX, 0, false);
                e.Handled = true;
            }
        }
#endif
    }
}

