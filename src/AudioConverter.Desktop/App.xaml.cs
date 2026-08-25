using System.Windows;
using AudioConverter.Desktop.Services;
using AudioConverter.Desktop.ViewModels;
namespace AudioConverter.Desktop;
public partial class App : Application
{
    private const string MutexName = "Local\\StealthyLabs.AudioConverter.SingleInstance";
    private const string ActivationEventName = "Local\\StealthyLabs.AudioConverter.Activate";
    private Mutex? instanceMutex;
    private EventWaitHandle? activationEvent;
    private readonly CancellationTokenSource shutdown = new();
    private bool ownsMutex;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        instanceMutex = new Mutex(true, MutexName, out var isFirstInstance);
        ownsMutex = isFirstInstance;
        if (!isFirstInstance)
        {
            try { EventWaitHandle.OpenExisting(ActivationEventName).Set(); }
            catch (WaitHandleCannotBeOpenedException) { }
            Shutdown();
            return;
        }

        activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivationEventName);
        var viewModel = new MainViewModel(new DialogService());
        var window = new MainWindow { DataContext = viewModel };
        MainWindow = window;
        window.Show();
        _ = ListenForActivationAsync(window, shutdown.Token);
        await viewModel.InitializeAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        shutdown.Cancel();
        activationEvent?.Set();
        activationEvent?.Dispose();
        if (ownsMutex) instanceMutex?.ReleaseMutex();
        instanceMutex?.Dispose();
        shutdown.Dispose();
        base.OnExit(e);
    }

    private async Task ListenForActivationAsync(Window window, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                activationEvent?.WaitOne();
                if (cancellationToken.IsCancellationRequested) break;
                Dispatcher.Invoke(() =>
                {
                    if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;
                    window.Show();
                    window.Activate();
                    window.Topmost = true;
                    window.Topmost = false;
                    window.Focus();
                });
            }
        }, cancellationToken);
    }
}
