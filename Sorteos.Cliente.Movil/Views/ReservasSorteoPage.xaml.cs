using Sorteos.Cliente.Movil.ViewModels;

namespace Sorteos.Cliente.Movil.Views
{
    public partial class ReservasSorteoPage : ContentPage
    {
        public ReservasSorteoPage(ReservasSorteoViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            viewModel.SolicitarCapturaFlyerAsync = CapturarFlyerAsync;
        }

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

