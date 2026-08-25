using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using AudioConverter.Core.Compression;
using AudioConverter.Core.Models;
using AudioConverter.Core.Paths;
using AudioConverter.Core.Progress;
using AudioConverter.Desktop.Mvvm;
using AudioConverter.Desktop.Services;
using AudioConverter.Infrastructure.Audio;
using AudioConverter.Infrastructure.Ffmpeg;
using AudioConverter.Infrastructure.Storage;
using AudioConverter.Infrastructure.Shell;

namespace AudioConverter.Desktop.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly DialogService dialogs;
    private readonly JsonSettingsStore settingsStore = new();
    private readonly WavePreviewPlayer previewPlayer = new();
    private readonly Stopwatch operationTimer = new();
    private FfmpegTools? tools;
    private AudioProcessingService? audio;
    private FfprobeService? probe;
    private string activeWorkspace = "Convert";
    private bool isBusy;
    private bool ffmpegMissing;
    private string theme = "Oled";
    private string sourcePath = "";
    private string outputFolder = "";
    private string suffix = "";
    private AudioFormat selectedFormat = AudioFormat.Mp3;
    private string bitrate = "192k";
    private string sampleRate = "Auto";
    private string bitDepth = "Auto";
    private bool preserveMetadata = true;
    private bool overwrite;
    private double progress;
    private string status = "Ready";
    private string report = "No conversion has run yet.";
    private string timingText = "";
    private string cutSourcePath = "";
    private string cutOutputFolder = "";
    private string cutSuffix = "_trimmed";
    private AudioFormat cutFormat = AudioFormat.Flac;
    private double durationSeconds;
    private double startSeconds;
    private double endSeconds;
    private bool fadeInEnabled;
    private bool fadeOutEnabled;
    private double fadeInSeconds = 1;
    private double fadeOutSeconds = 1;
    private double[] waveformPeaks = [];
    private string compressionSourcePath = "";
    private string compressionOutputFolder = "";
    private string compressionSuffix = "";
    private AudioFormat compressionFormat = AudioFormat.Mp3;
    private string compressionProfile = "High fidelity";
    private string compressionTargetTotalMb = "100";
    private bool compressionPreserveMetadata = true;
    private bool compressionOptimizeArtwork = true;
    private bool compressionOverwrite;
    private string compressionEstimate = "Choose a source, then analyze it to estimate the output size.";
    private string compressionReport = "No compression has run yet.";

    public MainViewModel(DialogService dialogs)
    {
        this.dialogs = dialogs;
        ShowConvertCommand = new RelayCommand(() => SetWorkspace("Convert"));
        ShowCutCommand = new RelayCommand(() => SetWorkspace("Cut"));
        ShowCompressCommand = new RelayCommand(() => SetWorkspace("Compress"));
        ShowAboutCommand = new RelayCommand(() => SetWorkspace("About"));
        ToggleThemeCommand = new AsyncRelayCommand(ToggleThemeAsync);
        ChooseFileCommand = new RelayCommand(ChooseFile);
        ChooseFolderCommand = new RelayCommand(ChooseFolder);
        ChooseOutputCommand = new RelayCommand(ChooseOutputFolder);
        ConvertCommand = new AsyncRelayCommand(ConvertAsync, () => !IsBusy && !string.IsNullOrWhiteSpace(SourcePath));
        InstallFfmpegCommand = new AsyncRelayCommand(InstallFfmpegAsync, () => !IsBusy);
        ChooseCutFileCommand = new AsyncRelayCommand(ChooseCutFileAsync, () => !IsBusy);
        ChooseCutOutputCommand = new RelayCommand(() => CutOutputFolder = dialogs.ChooseFolder(CutOutputFolder) ?? CutOutputFolder);
        CutCommand = new AsyncRelayCommand(CutAsync, () => !IsBusy && DurationSeconds > 0);
        PreviewCommand = new AsyncRelayCommand(PreviewAsync, () => !IsBusy && DurationSeconds > 0);
        StopPreviewCommand = new RelayCommand(() => { previewPlayer.Stop(); Status = "Preview stopped."; });
        ChooseCompressionFileCommand = new AsyncRelayCommand(ChooseCompressionFileAsync, () => !IsBusy);
        ChooseCompressionFolderCommand = new AsyncRelayCommand(ChooseCompressionFolderAsync, () => !IsBusy);
        ChooseCompressionOutputCommand = new RelayCommand(() => CompressionOutputFolder = dialogs.ChooseFolder(CompressionOutputFolder) ?? CompressionOutputFolder);
        AnalyzeCompressionCommand = new AsyncRelayCommand(AnalyzeCompressionAsync, () => !IsBusy && !string.IsNullOrWhiteSpace(CompressionSourcePath));
        CompressCommand = new AsyncRelayCommand(CompressAsync, () => !IsBusy && !string.IsNullOrWhiteSpace(CompressionSourcePath));
        OpenGitHubCommand = new RelayCommand(OpenGitHub);
    }

    public IReadOnlyList<AudioFormat> Formats => AudioFormats.All;
    public IReadOnlyList<string> Bitrates { get; } = ["96k", "128k", "160k", "192k", "256k", "320k"];
    public IReadOnlyList<string> SampleRates { get; } = ["Auto", "44100", "48000", "88200", "96000", "192000"];
    public IReadOnlyList<string> BitDepths { get; } = ["Auto", "16", "24", "32"];
    public bool IsConvertPage => activeWorkspace == "Convert";
    public bool IsCutPage => activeWorkspace == "Cut";
    public bool IsCompressionPage => activeWorkspace == "Compress";
    public bool IsAboutPage => activeWorkspace == "About";
    public bool IsToolWorkspace => !IsAboutPage;
    public string ConvertTabState => IsConvertPage ? "Active" : "Inactive";
    public string CutTabState => IsCutPage ? "Active" : "Inactive";
    public string CompressionTabState => IsCompressionPage ? "Active" : "Inactive";
    public string AboutTabState => IsAboutPage ? "Active" : "Inactive";
    public string ProductVersion => typeof(MainViewModel).Assembly.GetName().Version?.ToString(3) ?? "2.0.0";
    public bool IsBusy { get => isBusy; private set { if (Set(ref isBusy, value)) RaiseCommands(); } }
    public bool FfmpegMissing { get => ffmpegMissing; private set => Set(ref ffmpegMissing, value); }
    public string Theme { get => theme; private set { if (Set(ref theme, value)) Raise(nameof(ThemeButtonText)); } }
    public string ThemeButtonText => Theme.Equals("Oled", StringComparison.OrdinalIgnoreCase) ? "WHITE" : "OLED";
    public string SourcePath { get => sourcePath; set { if (Set(ref sourcePath, value)) RaiseCommands(); } }
    public string OutputFolder { get => outputFolder; set => Set(ref outputFolder, value); }
    public string Suffix { get => suffix; set => Set(ref suffix, value); }
    public AudioFormat SelectedFormat { get => selectedFormat; set { if (Set(ref selectedFormat, value)) { Raise(nameof(IsLossyFormat)); Raise(nameof(IsLosslessFormat)); } } }
    public bool IsLossyFormat => SelectedFormat.IsLossy();
    public bool IsLosslessFormat => !IsLossyFormat;
    public string Bitrate { get => bitrate; set => Set(ref bitrate, value); }
    public string SampleRate { get => sampleRate; set => Set(ref sampleRate, value); }
    public string BitDepth { get => bitDepth; set => Set(ref bitDepth, value); }
    public bool PreserveMetadata { get => preserveMetadata; set => Set(ref preserveMetadata, value); }
    public bool Overwrite { get => overwrite; set => Set(ref overwrite, value); }
    public double Progress { get => progress; private set => Set(ref progress, value); }
    public string Status { get => status; private set => Set(ref status, value); }
    public string Report { get => report; private set => Set(ref report, value); }
    public string TimingText { get => timingText; private set => Set(ref timingText, value); }
    public string CutSourcePath { get => cutSourcePath; set => Set(ref cutSourcePath, value); }
    public string CutOutputFolder { get => cutOutputFolder; set => Set(ref cutOutputFolder, value); }
    public string CutSuffix { get => cutSuffix; set => Set(ref cutSuffix, value); }
    public AudioFormat CutFormat { get => cutFormat; set => Set(ref cutFormat, value); }
    public double DurationSeconds { get => durationSeconds; private set { if (Set(ref durationSeconds, value)) RaiseCommands(); } }
    public double StartSeconds { get => startSeconds; set { var next = Math.Clamp(value, 0, Math.Max(0, EndSeconds - 0.001)); Set(ref startSeconds, next); Raise(nameof(SelectionDuration)); } }
    public double EndSeconds { get => endSeconds; set { var next = Math.Clamp(value, Math.Min(DurationSeconds, StartSeconds + 0.001), DurationSeconds); Set(ref endSeconds, next); Raise(nameof(SelectionDuration)); } }
    public double SelectionDuration => Math.Max(0, EndSeconds - StartSeconds);
    public bool FadeInEnabled { get => fadeInEnabled; set => Set(ref fadeInEnabled, value); }
    public bool FadeOutEnabled { get => fadeOutEnabled; set => Set(ref fadeOutEnabled, value); }
    public double FadeInSeconds { get => fadeInSeconds; set => Set(ref fadeInSeconds, value); }
    public double FadeOutSeconds { get => fadeOutSeconds; set => Set(ref fadeOutSeconds, value); }
    public double[] WaveformPeaks { get => waveformPeaks; private set => Set(ref waveformPeaks, value); }
    public IReadOnlyList<AudioFormat> CompressionFormats { get; } = [AudioFormat.Mp3, AudioFormat.Flac, AudioFormat.Ogg, AudioFormat.Aac, AudioFormat.M4a];
    public IReadOnlyList<string> CompressionProfiles => CompressionFormat == AudioFormat.Flac
        ? ["High fidelity", "Balanced", "Maximum reduction"]
        : ["High fidelity", "Balanced", "Maximum reduction", "Target total size"];
    public string CompressionSourcePath { get => compressionSourcePath; set { if (Set(ref compressionSourcePath, value)) RaiseCommands(); } }
    public string CompressionOutputFolder { get => compressionOutputFolder; set => Set(ref compressionOutputFolder, value); }
    public string CompressionSuffix { get => compressionSuffix; set => Set(ref compressionSuffix, value); }
    public AudioFormat CompressionFormat
    {
        get => compressionFormat;
        set
        {
            if (!Set(ref compressionFormat, value)) return;
            if (value == AudioFormat.Flac && SelectedCompressionProfile == "Target total size") SelectedCompressionProfile = "High fidelity";
            Raise(nameof(CompressionProfiles));
            Raise(nameof(IsTargetSizeCompression));
        }
    }
    public string SelectedCompressionProfile { get => compressionProfile; set { if (Set(ref compressionProfile, value)) Raise(nameof(IsTargetSizeCompression)); } }
    public bool IsTargetSizeCompression => SelectedCompressionProfile == "Target total size";
    public string CompressionTargetTotalMb { get => compressionTargetTotalMb; set => Set(ref compressionTargetTotalMb, value); }
    public bool CompressionPreserveMetadata { get => compressionPreserveMetadata; set => Set(ref compressionPreserveMetadata, value); }
    public bool CompressionOptimizeArtwork { get => compressionOptimizeArtwork; set => Set(ref compressionOptimizeArtwork, value); }
    public bool CompressionOverwrite { get => compressionOverwrite; set => Set(ref compressionOverwrite, value); }
    public string CompressionEstimate { get => compressionEstimate; private set => Set(ref compressionEstimate, value); }
    public string CompressionReport { get => compressionReport; private set => Set(ref compressionReport, value); }

    public RelayCommand ShowConvertCommand { get; }
    public RelayCommand ShowCutCommand { get; }
    public RelayCommand ShowCompressCommand { get; }
    public RelayCommand ShowAboutCommand { get; }
    public AsyncRelayCommand ToggleThemeCommand { get; }
    public RelayCommand ChooseFileCommand { get; }
    public RelayCommand ChooseFolderCommand { get; }
    public RelayCommand ChooseOutputCommand { get; }
    public AsyncRelayCommand ConvertCommand { get; }
    public AsyncRelayCommand InstallFfmpegCommand { get; }
    public AsyncRelayCommand ChooseCutFileCommand { get; }
    public RelayCommand ChooseCutOutputCommand { get; }
    public AsyncRelayCommand CutCommand { get; }
    public AsyncRelayCommand PreviewCommand { get; }
    public RelayCommand StopPreviewCommand { get; }
    public AsyncRelayCommand ChooseCompressionFileCommand { get; }
    public AsyncRelayCommand ChooseCompressionFolderCommand { get; }
    public RelayCommand ChooseCompressionOutputCommand { get; }
    public AsyncRelayCommand AnalyzeCompressionCommand { get; }
    public AsyncRelayCommand CompressCommand { get; }
    public RelayCommand OpenGitHubCommand { get; }

    public async Task InitializeAsync()
    {
        var settings = await settingsStore.LoadAsync();
        Theme = settings.Theme; OutputFolder = settings.LastOutputDirectory ?? ""; ThemeService.Apply(Theme);
        TryLocateFfmpeg();
    }

    public async Task AcceptDropAsync(IReadOnlyList<string> paths)
    {
        var path = paths.FirstOrDefault(item => File.Exists(item) || Directory.Exists(item));
        if (path is null) return;
        if (IsConvertPage) SourcePath = path;
        else if (IsCutPage && File.Exists(path)) await LoadCutSourceAsync(path);
        else if (IsCompressionPage)
        {
            CompressionSourcePath = path;
            await AnalyzeCompressionAsync();
        }
    }

    private void SetWorkspace(string workspace)
    {
        if (!Set(ref activeWorkspace, workspace, nameof(activeWorkspace))) return;
        Raise(nameof(IsConvertPage));
        Raise(nameof(IsCutPage));
        Raise(nameof(IsCompressionPage));
        Raise(nameof(IsAboutPage));
        Raise(nameof(IsToolWorkspace));
        Raise(nameof(ConvertTabState));
        Raise(nameof(CutTabState));
        Raise(nameof(CompressionTabState));
        Raise(nameof(AboutTabState));
    }

    private void OpenGitHub()
    {
        try { ExternalLinkService.OpenGitHubProfile(); }
        catch (Exception error) { dialogs.Error($"Unable to open GitHub: {error.Message}"); }
    }

    private void TryLocateFfmpeg()
    {
        try
        {
            tools = FfmpegLocator.Find(); audio = new AudioProcessingService(tools); probe = new FfprobeService(tools.FfprobePath);
            FfmpegMissing = false; Status = "FFmpeg ready. All processing stays local.";
        }
        catch (FfmpegDependencyException error)
        {
            FfmpegMissing = true; Status = error.Message;
        }
    }

    private async Task InstallFfmpegAsync()
    {
        await RunBusyAsync(async () =>
        {
            var installer = new FfmpegInstaller();
            var installProgress = new Progress<(double Fraction, string Status)>(item => UpdateOperationProgress(item.Fraction, item.Status));
            tools = await installer.InstallAsync(progress: installProgress); audio = new AudioProcessingService(tools); probe = new FfprobeService(tools.FfprobePath); FfmpegMissing = false;
        });
    }

    private void ChooseFile() { var path = dialogs.ChooseAudioFile(); if (path is not null) SourcePath = path; }
    private void ChooseFolder() { var path = dialogs.ChooseFolder(); if (path is not null) SourcePath = path; }
    private void ChooseOutputFolder() { var path = dialogs.ChooseFolder(OutputFolder); if (path is not null) OutputFolder = path; }
    private async Task ChooseCutFileAsync() { var path = dialogs.ChooseAudioFile(); if (path is not null) await LoadCutSourceAsync(path); }
    private async Task ChooseCompressionFileAsync()
    {
        var path = dialogs.ChooseAudioFile();
        if (path is null) return;
        CompressionSourcePath = path;
        await AnalyzeCompressionAsync();
    }
    private async Task ChooseCompressionFolderAsync()
    {
        var path = dialogs.ChooseFolder();
        if (path is null) return;
        CompressionSourcePath = path;
        await AnalyzeCompressionAsync();
    }

    private async Task ConvertAsync()
    {
        await RunBusyAsync(async () =>
        {
            EnsureFfmpeg();
            var files = EnumerateSources(SourcePath).ToArray();
            if (files.Length == 0) throw new InvalidDataException("No supported audio file was found.");
            var results = new List<FileResult>();
            for (var index = 0; index < files.Length; index++)
            {
                var file = files[index];
                try
                {
                    _ = AudioFormats.FromPath(file);
                    var info = await probe!.ProbeAsync(file);
                    var directory = ResolveOutputDirectory(file, OutputFolder);
                    Directory.CreateDirectory(directory);
                    var output = OutputPathBuilder.Build(file, directory, SelectedFormat, Suffix);
                    var options = CreateOptions(SelectedFormat);
                    var itemProgress = new Progress<double>(fraction => UpdateOperationProgress((index + fraction) / files.Length, $"Converting {index + 1}/{files.Length}: {Path.GetFileName(file)}"));
                    await audio!.ConvertAsync(file, output, options, info.DurationSeconds, itemProgress);
                    results.Add(new FileResult(file, output, null));
                }
                catch (Exception error) { results.Add(new FileResult(file, null, error.Message)); }
            }
            var batch = new BatchResult(results);
            Report = $"Completed: {batch.Succeeded} succeeded, {batch.Failed} failed\n" + string.Join('\n', results.Select(result => result.Succeeded ? $"OK  {Path.GetFileName(result.InputPath)} -> {result.OutputPath}" : $"FAIL  {Path.GetFileName(result.InputPath)}: {result.Error}"));
            UpdateOperationProgress(1, $"Completed: {batch.Succeeded} succeeded, {batch.Failed} failed");
            await settingsStore.SaveAsync(new AppSettings(Theme, OutputFolder));
        });
    }

    private async Task LoadCutSourceAsync(string path)
    {
        await RunBusyAsync(async () =>
        {
            EnsureFfmpeg(); _ = AudioFormats.FromPath(path); Status = "Reading audio and generating waveform…";
            var info = await probe!.ProbeAsync(path); var peaks = await audio!.ExtractWaveformAsync(path, 900);
            CutSourcePath = path; DurationSeconds = info.DurationSeconds; StartSeconds = 0; EndSeconds = info.DurationSeconds;
            CutFormat = AudioFormats.FromPath(path); WaveformPeaks = peaks; UpdateOperationProgress(1, $"Ready to cut {Path.GetFileName(path)}");
        });
    }

    private async Task CutAsync()
    {
        await RunBusyAsync(async () =>
        {
            EnsureFfmpeg(); var trim = CreateTrim(); var directory = ResolveOutputDirectory(CutSourcePath, CutOutputFolder); Directory.CreateDirectory(directory);
            var output = OutputPathBuilder.Build(CutSourcePath, directory, CutFormat, CutSuffix);
            var options = new ConversionOptions(CutFormat, CutFormat.IsLossy() ? "192k" : null, PreserveMetadata: PreserveMetadata, Overwrite: Overwrite);
            var itemProgress = new Progress<double>(fraction => UpdateOperationProgress(fraction, "Cutting audio…"));
            await audio!.TrimAsync(CutSourcePath, output, options, trim, itemProgress);
            Report = $"Cut completed: {Path.GetFileName(output)}\nOK  {CutSourcePath} -> {output}";
            UpdateOperationProgress(1, $"Cut completed: {Path.GetFileName(output)}");
        });
    }

    private async Task PreviewAsync()
    {
        await RunBusyAsync(async () =>
        {
            EnsureFfmpeg(); previewPlayer.Stop(); var output = AppPaths.PreviewWave;
            Status = "Rendering local preview…";
            var previewProgress = new Progress<double>(fraction => UpdateOperationProgress(fraction, "Rendering local preview…"));
            await audio!.RenderPreviewAsync(CutSourcePath, output, CreateTrim(), previewProgress);
            previewPlayer.Play(output);
            UpdateOperationProgress(1, "Playing cut preview.");
        });
    }

    private async Task AnalyzeCompressionAsync()
    {
        await RunBusyAsync(async () =>
        {
            var preparation = await PrepareCompressionAsync(1);
            CompressionEstimate = BuildCompressionEstimate(preparation.Plan);
            CompressionReport = preparation.Failures.Count == 0
                ? "Analysis completed. No invalid files found."
                : string.Join('\n', preparation.Failures);
            UpdateOperationProgress(1, $"Compression analysis ready: {preparation.Plan.Files.Count} valid file(s)");
        });
    }

    private async Task CompressAsync()
    {
        await RunBusyAsync(async () =>
        {
            var preparation = await PrepareCompressionAsync(0.1);
            var plan = preparation.Plan;
            CompressionEstimate = BuildCompressionEstimate(plan);
            var activeFiles = plan.Files.Where(file => !file.ShouldSkip).ToArray();
            if (activeFiles.Length == 0)
            {
                CompressionReport = "All files were skipped because no meaningful size reduction was expected.";
                UpdateOperationProgress(1, "Nothing to compress.");
                return;
            }

            if (CompressionFormat.IsLossy()
                && activeFiles.Any(file => file.Source.Format.IsLossy())
                && !dialogs.Confirm("Some source files are already lossy. Re-encoding them can reduce audio quality. Continue?"))
            {
                Status = "Compression cancelled.";
                TimingText = "";
                return;
            }

            var reportLines = new List<string>(preparation.Failures);
            foreach (var skipped in plan.Files.Where(file => file.ShouldSkip))
                reportLines.Add($"SKIP  {skipped.Source.RelativePath}: {skipped.SkipReason}");

            var totalDuration = activeFiles.Sum(file => file.Source.DurationSeconds);
            var completedDuration = 0d;
            var succeeded = 0;
            var failed = preparation.Failures.Count;
            long originalProcessedBytes = 0;
            long outputBytes = 0;
            var activeIndex = 0;
            foreach (var file in activeFiles)
            {
                try
                {
                    var discovered = preparation.Discovered[file.Source.Path];
                    var output = OutputPathBuilder.BuildCompressed(
                        file.Source.Path,
                        discovered.SourceRoot,
                        preparation.OutputRoot,
                        CompressionFormat,
                        CompressionSuffix);
                    Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                    var baseDuration = completedDuration;
                    var progress = new Progress<double>(fraction =>
                    {
                        var audioFraction = totalDuration <= 0
                            ? 1
                            : (baseDuration + fraction * file.Source.DurationSeconds) / totalDuration;
                        UpdateOperationProgress(
                            0.1 + audioFraction * 0.9,
                            $"Compressing {activeIndex + 1}/{activeFiles.Length}: {Path.GetFileName(file.Source.Path)}");
                    });
                    await audio!.CompressAsync(
                        file.Source.Path,
                        output,
                        plan.Options,
                        plan.TargetAudioBitrateKbps,
                        file.Source.DurationSeconds,
                        file.Source.HasCoverArt,
                        progress);
                    completedDuration += file.Source.DurationSeconds;
                    originalProcessedBytes += file.Source.SizeBytes;
                    outputBytes += new FileInfo(output).Length;
                    succeeded++;
                    reportLines.Add($"OK    {file.Source.RelativePath} -> {output}");
                }
                catch (Exception error)
                {
                    completedDuration += file.Source.DurationSeconds;
                    failed++;
                    reportLines.Add($"FAIL  {file.Source.RelativePath}: {error.Message}");
                }
                activeIndex++;
            }

            var savings = CompressionSavings.Calculate(originalProcessedBytes, outputBytes);
            var skippedCount = plan.Files.Count(file => file.ShouldSkip);
            CompressionReport =
                $"Completed: {succeeded} succeeded, {failed} failed, {skippedCount} skipped\n" +
                $"Original processed: {FormatBytes(originalProcessedBytes)}\n" +
                $"Final: {FormatBytes(outputBytes)}\n" +
                $"Saved: {FormatBytes(savings.SavedBytes)} ({savings.ReductionPercent:0.0}%)\n\n" +
                string.Join('\n', reportLines);
            UpdateOperationProgress(1, $"Compression completed: {succeeded} succeeded, {failed} failed, {skippedCount} skipped");
        });
    }

    private async Task<CompressionPreparation> PrepareCompressionAsync(double progressWeight)
    {
        EnsureFfmpeg();
        var outputRoot = ResolveCompressionOutputRoot();
        var discovered = CompressionFileDiscovery.Find(CompressionSourcePath, recursive: true)
            .Where(file => !IsPathInside(file.Path, outputRoot))
            .ToArray();
        if (discovered.Length == 0) throw new InvalidDataException("No supported audio file was found.");

        var sources = new List<CompressionSource>();
        var failures = new List<string>();
        for (var index = 0; index < discovered.Length; index++)
        {
            var file = discovered[index];
            try
            {
                var info = await probe!.ProbeAsync(file.Path);
                sources.Add(new CompressionSource(
                    file.Path,
                    file.RelativePath,
                    AudioFormats.FromPath(file.Path),
                    info.DurationSeconds,
                    info.SizeBytes ?? new FileInfo(file.Path).Length,
                    info.AudioBitrateKbps,
                    info.HasCoverArt));
            }
            catch (Exception error)
            {
                failures.Add($"FAIL  {file.RelativePath}: {error.Message}");
            }
            UpdateOperationProgress(
                (index + 1d) / discovered.Length * progressWeight,
                $"Analyzing {index + 1}/{discovered.Length}: {Path.GetFileName(file.Path)}");
        }
        if (sources.Count == 0) throw new InvalidDataException("No valid audio file remained after ffprobe validation.");

        var options = new CompressionOptions(
            CompressionFormat,
            ParseCompressionProfile(SelectedCompressionProfile),
            ParseTargetBytes(),
            CompressionOptimizeArtwork,
            CompressionPreserveMetadata,
            CompressionOverwrite);
        var plan = CompressionPlanner.Create(sources, options);
        var lookup = discovered.ToDictionary(file => file.Path, StringComparer.OrdinalIgnoreCase);
        return new CompressionPreparation(plan, lookup, failures, outputRoot);
    }

    private string ResolveCompressionOutputRoot()
    {
        if (!string.IsNullOrWhiteSpace(CompressionOutputFolder)) return Path.GetFullPath(CompressionOutputFolder);
        return File.Exists(CompressionSourcePath)
            ? Path.Combine(Path.GetDirectoryName(Path.GetFullPath(CompressionSourcePath))!, "compressed")
            : Path.Combine(Path.GetFullPath(CompressionSourcePath), "compressed");
    }

    private long? ParseTargetBytes()
    {
        if (!IsTargetSizeCompression) return null;
        if (!double.TryParse(CompressionTargetTotalMb, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out var megabytes)
            && !double.TryParse(CompressionTargetTotalMb, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out megabytes))
            throw new ArgumentException("Target total size must be a number in MiB.");
        if (megabytes <= 0) throw new ArgumentException("Target total size must be positive.");
        return checked((long)(megabytes * 1024 * 1024));
    }

    private static CompressionProfile ParseCompressionProfile(string value) => value switch
    {
        "High fidelity" => AudioConverter.Core.Models.CompressionProfile.HighFidelity,
        "Balanced" => AudioConverter.Core.Models.CompressionProfile.Balanced,
        "Maximum reduction" => AudioConverter.Core.Models.CompressionProfile.MaximumReduction,
        "Target total size" => AudioConverter.Core.Models.CompressionProfile.TargetTotalSize,
        _ => throw new ArgumentException("Unknown compression profile."),
    };

    private static bool IsPathInside(string path, string directory)
    {
        var fullPath = Path.GetFullPath(path);
        var fullDirectory = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildCompressionEstimate(CompressionPlan plan)
    {
        var saved = Math.Max(0, plan.OriginalTotalBytes - plan.EstimatedOutputBytes);
        var reduction = plan.OriginalTotalBytes == 0 ? 0 : saved * 100d / plan.OriginalTotalBytes;
        var skipped = plan.Files.Count(file => file.ShouldSkip);
        var bitrate = plan.TargetAudioBitrateKbps is null ? "" : $" · {plan.TargetAudioBitrateKbps} kbps shared target";
        return $"Original {FormatBytes(plan.OriginalTotalBytes)} · estimated {FormatBytes(plan.EstimatedOutputBytes)} · save {reduction:0.0}%{bitrate} · {skipped} skipped";
    }

    private static string FormatBytes(long bytes) => $"{bytes / 1024d / 1024d:0.00} MiB";

    private sealed record CompressionPreparation(
        CompressionPlan Plan,
        IReadOnlyDictionary<string, CompressionDiscoveredFile> Discovered,
        IReadOnlyList<string> Failures,
        string OutputRoot);

    private TrimSelection CreateTrim() => TrimSelection.Create(StartSeconds, EndSeconds, FadeInEnabled ? FadeInSeconds : 0, FadeOutEnabled ? FadeOutSeconds : 0);
    private ConversionOptions CreateOptions(AudioFormat format) => new(
        format,
        format.IsLossy() ? Bitrate : null,
        format.IsLossy() ? null : ParseOptionalInt(SampleRate),
        format.IsLossy() ? null : ParseOptionalInt(BitDepth),
        PreserveMetadata,
        Overwrite);
    private static int? ParseOptionalInt(string value) => int.TryParse(value, out var result) ? result : null;
    private static string ResolveOutputDirectory(string source, string configured) => string.IsNullOrWhiteSpace(configured) ? Path.Combine(Path.GetDirectoryName(source)!, "converted") : configured;
    private static IEnumerable<string> EnumerateSources(string source) => File.Exists(source) ? [source] : Directory.Exists(source) ? Directory.EnumerateFiles(source).Where(path => { try { _ = AudioFormats.FromPath(path); return true; } catch { return false; } }) : [];
    private void EnsureFfmpeg() { if (tools is null || audio is null || probe is null) throw new FfmpegDependencyException("FFmpeg is required. Install it from this application first."); }

    private async Task RunBusyAsync(Func<Task> action)
    {
        if (IsBusy) return;
        IsBusy = true;
        Progress = 0;
        TimingText = "Estimating time remaining…";
        operationTimer.Restart();
        var succeeded = false;
        try
        {
            await action();
            succeeded = true;
        }
        catch (Exception error)
        {
            operationTimer.Stop();
            TimingText = $"Stopped after {OperationTiming.FormatCompact(operationTimer.Elapsed)}";
            Status = error.Message;
            dialogs.Error(error.Message);
        }
        finally
        {
            operationTimer.Stop();
            if (succeeded && Progress >= 100)
            {
                TimingText = $"Completed in {OperationTiming.FormatCompact(operationTimer.Elapsed)}";
            }
            IsBusy = false;
        }
    }

    private void UpdateOperationProgress(double completedFraction, string statusText)
    {
        var fraction = Math.Clamp(completedFraction, 0, 1);
        Progress = fraction * 100;
        Status = statusText;
        if (fraction >= 1)
        {
            TimingText = $"Completed in {OperationTiming.FormatCompact(operationTimer.Elapsed)}";
            return;
        }

        var remaining = OperationTiming.EstimateRemaining(operationTimer.Elapsed, fraction);
        if (remaining is null || operationTimer.Elapsed < TimeSpan.FromSeconds(2))
        {
            TimingText = "Estimating time remaining…";
            return;
        }

        var finishTime = DateTime.Now.Add(remaining.Value);
        TimingText = $"{OperationTiming.FormatCompact(remaining.Value)} remaining · finishes around {finishTime:HH:mm}";
    }

    private async Task ToggleThemeAsync()
    {
        Theme = Theme.Equals("Oled", StringComparison.OrdinalIgnoreCase) ? "White" : "Oled"; ThemeService.Apply(Theme); await settingsStore.SaveAsync(new AppSettings(Theme, OutputFolder));
    }

    private void RaiseCommands() { ConvertCommand.RaiseCanExecuteChanged(); CutCommand.RaiseCanExecuteChanged(); PreviewCommand.RaiseCanExecuteChanged(); InstallFfmpegCommand.RaiseCanExecuteChanged(); ChooseCutFileCommand.RaiseCanExecuteChanged(); ChooseCompressionFileCommand.RaiseCanExecuteChanged(); AnalyzeCompressionCommand.RaiseCanExecuteChanged(); CompressCommand.RaiseCanExecuteChanged(); }
    public void Dispose() => previewPlayer.Dispose();
}
