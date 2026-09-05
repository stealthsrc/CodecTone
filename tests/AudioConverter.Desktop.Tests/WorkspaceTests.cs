using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using AudioConverter.Core.Remix;
using AudioConverter.Desktop;
using AudioConverter.Desktop.Services;
using AudioConverter.Desktop.ViewModels;

namespace AudioConverter.Desktop.Tests;

[TestClass]
public sealed class WorkspaceTests
{
    [TestMethod]
    public async Task EditedRack_RefusalPreservesIntensityAndValues()
    {
        await OnSta(async () =>
        {
            var dialogs = new TestDialogs();
            using var vm = new MainViewModel(dialogs);
            vm.ApplyRemixPresetCommand.Execute(RemixPresetCatalog.All.Single(p => p.Preset == RemixPreset.BassBoost));
            vm.RemixEffects[0].First = 3;
            vm.SelectedRemixIntensity = RemixIntensity.Strong;
            Assert.AreEqual(1, dialogs.Confirmations);
            Assert.AreEqual(RemixIntensity.Medium, vm.SelectedRemixIntensity);
            Assert.AreEqual(3d, vm.RemixEffects[0].First);
            await vm.ShutdownAsync();
        });
    }

    [TestMethod]
    public async Task Shutdown_WaitsForOperationCleanupAndKeepsPartialReport()
    {
        await OnSta(async () =>
        {
            using var vm = new MainViewModel(new TestDialogs());
            var release = new TaskCompletionSource();
            var busy = (Task)typeof(MainViewModel).GetMethod("RunBusyAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(vm, [new Func<Task>(async () => { await release.Task; throw new OperationCanceledException(); })])!;
            var shutdown = vm.ShutdownAsync();
            Assert.IsFalse(shutdown.IsCompleted);
            Assert.IsFalse(vm.IsWorkspaceEnabled);
            release.SetResult();
            await shutdown;
            await busy;
            StringAssert.Contains(vm.Report, "CANCELLED");
        });
    }

    [TestMethod]
    public async Task PreviewVolume_DoesNotMutateRack()
    {
        await OnSta(async () =>
        {
            using var vm = new MainViewModel(new TestDialogs());
            vm.ApplyRemixPresetCommand.Execute(RemixPresetCatalog.All.Single(p => p.Preset == RemixPreset.BassBoost));
            var before = vm.RemixEffects.Select(e => e.ToModel()).ToArray();
            vm.PreviewVolume = 10;
            CollectionAssert.AreEqual(before, vm.RemixEffects.Select(e => e.ToModel()).ToArray());
            await vm.ShutdownAsync();
        });
    }

    [TestMethod]
    public async Task WorkspaceLayouts_RenderBothThemesAtMinimumSize()
    {
        await OnSta(async () =>
        {
            var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            foreach (var theme in new[] { "Oled", "White" })
            foreach (var workspace in new[] { "Remix", "Artwork" })
            {
                application.Resources.MergedDictionaries.Clear();
                application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/CodecTone;component/Themes/{theme}.xaml") });
                application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/CodecTone;component/Themes/Controls.xaml") });
                using var vm = new MainViewModel(new TestDialogs());
                if (workspace == "Remix") vm.ShowRemixCommand.Execute(null); else vm.ShowArtworkCommand.Execute(null);
                var window = new MainWindow { DataContext = vm };
                var root = (FrameworkElement)window.Content;
                root.Measure(new Size(900,650));
                root.Arrange(new Rect(0,0,900,650));
                root.UpdateLayout();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                Assert.AreEqual(900d, root.ActualWidth);
                var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(900,650,96,96,System.Windows.Media.PixelFormats.Pbgra32);
                bitmap.Render(root);
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                using (var stream = System.IO.File.Create(System.IO.Path.Combine(AppContext.BaseDirectory, $"layout-{theme}-{workspace}.png"))) encoder.Save(stream);
                await vm.ShutdownAsync();
                window.Close();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            }
            application.Shutdown();
        });
    }

    private static Task OnSta(Func<Task> action)
    {
        var result = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
            dispatcher.BeginInvoke(new Action(async () =>
            {
                try { await action(); result.SetResult(); }
                catch (Exception error) { result.SetException(error); }
                finally { dispatcher.BeginInvokeShutdown(DispatcherPriority.Background); }
            }));
            Dispatcher.Run();
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return result.Task;
    }

    private sealed class TestDialogs : IDialogService
    {
        public int Confirmations { get; private set; }
        public string? ChooseAudioFile() => null;
        public string? ChooseFolder(string? initialDirectory = null) => null;
        public string? ChooseCoverImage() => null;
        public void Error(string message) => Assert.Fail(message);
        public bool Confirm(string message) { Confirmations++; return false; }
    }
}
