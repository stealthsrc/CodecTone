using System.ComponentModel;
using System.Windows;
using AudioConverter.Desktop.ViewModels;

namespace AudioConverter.Desktop;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && e.Data.GetData(DataFormats.FileDrop) is string[] paths)
        {
            await viewModel.AcceptDropAsync(paths);
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is IDisposable disposable) disposable.Dispose();
    }
}
