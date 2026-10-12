using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AntonioAguadoRovira.NINA.Rgbfocus.RgbfocusDockables {
    [Export(typeof(ResourceDictionary))]
    public partial class MyPluginDockableTemplates : ResourceDictionary {
        private bool panning;
        private Point panStart;
        private double panStartH;
        private double panStartV;

        public MyPluginDockableTemplates() {
            InitializeComponent();
        }

        // ---- Botones de zoom (el Tag de cada botón apunta al ScrollViewer del visor) ----

        private void ZoomIn_Click(object sender, RoutedEventArgs e) => ZoomByButton(sender, 1.25);

        private void ZoomOut_Click(object sender, RoutedEventArgs e) => ZoomByButton(sender, 0.8);

        private void Fit_Click(object sender, RoutedEventArgs e) {
            if (GetViewer(sender, out _, out var vm)) vm.FitToWindow();
        }

        private void OneToOne_Click(object sender, RoutedEventArgs e) {
            if (GetViewer(sender, out var sv, out var vm)) {
                ZoomAbout(sv, vm, 1.0, new Point(sv.ViewportWidth / 2, sv.ViewportHeight / 2));
            }
        }

        private void ZoomByButton(object sender, double factor) {
            if (GetViewer(sender, out var sv, out var vm)) {
                ZoomAbout(sv, vm, vm.Zoom * factor, new Point(sv.ViewportWidth / 2, sv.ViewportHeight / 2));
            }
        }

        private static bool GetViewer(object sender, out ScrollViewer sv, out RgbfocusDockable vm) {
            sv = (sender as FrameworkElement)?.Tag as ScrollViewer;
            vm = sv?.DataContext as RgbfocusDockable;
            return sv != null && vm != null;
        }

        /// <summary>Cambia el zoom manteniendo fijo el punto de la imagen que está bajo 'anchor' (coordenadas del visor).</summary>
        private static void ZoomAbout(ScrollViewer sv, RgbfocusDockable vm, double newZoom, Point anchor) {
            double oldScale = vm.DisplayScale;
            if (oldScale <= 0) {
                vm.ZoomTo(newZoom);
                return;
            }

            double contentX = (sv.HorizontalOffset + anchor.X) / oldScale;
            double contentY = (sv.VerticalOffset + anchor.Y) / oldScale;

            vm.ZoomTo(newZoom);
            sv.UpdateLayout();

            double newScale = vm.DisplayScale;
            sv.ScrollToHorizontalOffset(contentX * newScale - anchor.X);
            sv.ScrollToVerticalOffset(contentY * newScale - anchor.Y);
        }

        // ---- Visor: tamaño, rueda del ratón y arrastre ----

        private void Viewer_SizeChanged(object sender, SizeChangedEventArgs e) {
            if (sender is ScrollViewer sv && sv.DataContext is RgbfocusDockable vm) {
                // Pequeño margen para que no aparezcan barras de desplazamiento al ajustar a la ventana
                vm.SetViewport(Math.Max(0, e.NewSize.Width - 4), Math.Max(0, e.NewSize.Height - 4));
            }
        }

        private void Viewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
            if (sender is ScrollViewer sv && sv.DataContext is RgbfocusDockable vm) {
                double factor = e.Delta > 0 ? 1.25 : 0.8;
                ZoomAbout(sv, vm, vm.Zoom * factor, e.GetPosition(sv));
                e.Handled = true;
            }
        }

        private void Viewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            // Solo se arrastra si se pincha sobre la imagen o sobre la rejilla de zonas (no sobre las barras de desplazamiento)
            bool overContent = e.OriginalSource is Image || (e.OriginalSource as FrameworkElement)?.DataContext is ZoneCell;
            if (sender is ScrollViewer sv && overContent) {
                panning = true;
                panStart = e.GetPosition(sv);
                panStartH = sv.HorizontalOffset;
                panStartV = sv.VerticalOffset;
                sv.CaptureMouse();
                sv.Cursor = Cursors.SizeAll;
            }
        }

        private void Viewer_PreviewMouseMove(object sender, MouseEventArgs e) {
            if (panning && sender is ScrollViewer sv) {
                var p = e.GetPosition(sv);
                sv.ScrollToHorizontalOffset(panStartH - (p.X - panStart.X));
                sv.ScrollToVerticalOffset(panStartV - (p.Y - panStart.Y));
            }
        }

        private void Viewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) => EndPan(sender);

        private void Viewer_LostMouseCapture(object sender, MouseEventArgs e) => EndPan(sender);

        private void EndPan(object sender) {
            if (!panning) return;
            panning = false;
            if (sender is ScrollViewer sv) {
                sv.ReleaseMouseCapture();
                sv.Cursor = Cursors.Arrow;
            }
        }
    }
}
