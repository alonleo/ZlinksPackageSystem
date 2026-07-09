using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ZlinksPackageSystem.Desktop.ViewModels;

namespace ZlinksPackageSystem.Desktop.Views
{
    public partial class MainWindow : Window
    {
        private WindowEdge? _currentEdge;

        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(MainViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
            {
                if (e.ClickCount == 2)
                {
                    WindowState = WindowState == WindowState.Maximized
                        ? WindowState.Normal
                        : WindowState.Maximized;
                }
                else
                {
                    BeginMoveDrag(e);
                }
            }
        }

        private void Window_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (WindowState == WindowState.Maximized) return;

            var pos = e.GetPosition(this);
            var edge = GetResizeEdge(pos);
            _currentEdge = edge;

            Cursor = edge switch
            {
                null => Cursor.Default,
                WindowEdge.NorthWest or WindowEdge.SouthEast => new Cursor(StandardCursorType.TopLeftCorner),
                WindowEdge.NorthEast or WindowEdge.SouthWest => new Cursor(StandardCursorType.TopRightCorner),
                WindowEdge.North or WindowEdge.South => new Cursor(StandardCursorType.SizeNorthSouth),
                WindowEdge.West or WindowEdge.East => new Cursor(StandardCursorType.SizeWestEast),
                _ => Cursor.Default
            };
        }

        private void Window_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (WindowState == WindowState.Maximized) return;
            if (_currentEdge == null) return;

            var point = e.GetCurrentPoint(this);
            if (point.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
            {
                BeginResizeDrag(_currentEdge.Value, e);
            }
        }

        private WindowEdge? GetResizeEdge(Point pos)
        {
            const int threshold = 6;
            WindowEdge? edge = null;
            var w = Bounds.Width;
            var h = Bounds.Height;

            if (pos.X <= threshold) edge = WindowEdge.West;
            else if (pos.X >= w - threshold) edge = WindowEdge.East;

            if (pos.Y <= threshold)
                edge = edge == WindowEdge.West ? WindowEdge.NorthWest
                     : edge == WindowEdge.East ? WindowEdge.NorthEast
                     : WindowEdge.North;
            else if (pos.Y >= h - threshold)
                edge = edge == WindowEdge.West ? WindowEdge.SouthWest
                     : edge == WindowEdge.East ? WindowEdge.SouthEast
                     : WindowEdge.South;

            return edge;
        }

        private void MinimizeButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void CloseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }
    }
}
