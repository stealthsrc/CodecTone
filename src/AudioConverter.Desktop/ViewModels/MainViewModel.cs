using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using AudioConverter.Core.Compression;
using AudioConverter.Core.Models;
using AudioConverter.Core.Paths;
using AudioConverter.Core.Progress;
using AudioConverter.Core.Remix;
using AudioConverter.Core.Validation;
using AudioConverter.Desktop.Mvvm;
using AudioConverter.Desktop.Services;
using AudioConverter.Infrastructure.Audio;
using AudioConverter.Infrastructure.Ffmpeg;
using AudioConverter.Infrastructure.Storage;
using AudioConverter.Infrastructure.Shell;
using AudioConverter.Infrastructure.Remix;

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
    private RemixProcessingService? remix;
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
    private string remixSourcePath = "";
    private double remixDurationSeconds;
    private int remixSampleRate = 44_100;
    private bool remixSourceHasCover;
    private double[] remixWaveformPeaks = [];
    private double remixPreviewStartSeconds;
    private AudioFormat remixFormat = AudioFormat.Mp3;
    private string remixBitrate = "192k";
    private string remixSampleRateOption = "Auto";
    private string remixBitDepth = "Auto";
    private string remixOutputFolder = "";
    private string remixSuffix = "_remix";
    private bool remixOverwrite;
    private RemixEffectKind selectedRemixEffectKind = RemixEffectKind.Bass;
    private string remixInspectorTab = "Export";
    private bool remixRackDirty;
    private string remixTitle = "";
    private string remixArtist = "";
    private string remixAlbum = "";
    private string remixAlbumArtist = "";
    private string remixGenre = "";
    private string remixDate = "";
    private string remixTrack = "";
    private string remixDisc = "";
    private string remixComment = "";
    private string remixCoverAction = "Keep";
    private string remixCoverPath = "";
    private string remixReport = "No remix has been exported yet.";

    public MainViewModel(DialogService dialogs)
    {
        this.dialogs = dialogs;
        ShowConvertCommand = new RelayCommand(() => SetWorkspace("Convert"));
        ShowCutCommand = new RelayCommand(() => SetWorkspace("Cut"));
        ShowCompressCommand = new RelayCommand(() => SetWorkspace("Compress"));
        ShowAboutCommand = new RelayCommand(() => SetWorkspace("About"));
        ShowRemixCommand = new RelayCommand(() => SetWorkspace("Remix"));
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
        ChooseRemixFileCommand = new AsyncRelayCommand(ChooseRemixFileAsync, () => !IsBusy);
        ApplyRemixPresetCommand = new RelayCommand<string>(ApplyRemixPreset);
        AddRemixEffectCommand = new RelayCommand(AddRemixEffect);
        RemoveRemixEffectCommand = new RelayCommand<RemixEffectViewModel>(RemoveRemixEffect);
        MoveRemixEffectUpCommand = new RelayCommand<RemixEffectViewModel>(effect => MoveRemixEffect(effect, -1));
        MoveRemixEffectDownCommand = new RelayCommand<RemixEffectViewModel>(effect => MoveRemixEffect(effect, 1));
        ShowRemixInspectorCommand = new RelayCommand<string>(tab => RemixInspectorTab = tab);
        ChooseRemixOutputCommand = new RelayCommand(() => RemixOutputFolder = dialogs.ChooseFolder(RemixOutputFolder) ?? RemixOutputFolder);
        ChooseRemixCoverCommand = new RelayCommand(ChooseRemixCover);
        RemoveRemixCoverCommand = new RelayCommand(() => { RemixCoverAction = "Remove"; RemixCoverPath = ""; });
        AddMetadataTagCommand = new RelayCommand(() => RemixCustomTags.Add(new MetadataTagViewModel()));
        RemoveMetadataTagCommand = new RelayCommand<MetadataTagViewModel>(tag => RemixCustomTags.Remove(tag));
        PreviewRemixCommand = new AsyncRelayCommand(PreviewRemixAsync, () => !IsBusy && RemixDurationSeconds > 0);
        StopRemixCommand = new RelayCommand(() => { previewPlayer.Stop(); Status = "Remix preview stopped."; });
        ExportRemixCommand = new AsyncRelayCommand(ExportRemixAsync, () => !IsBusy && RemixDurationSeconds > 0);
    }

    public IReadOnlyList<AudioFormat> Formats => AudioFormats.All;
    public IReadOnlyList<string> Bitrates { get; } = ["96k", "128k", "160k", "192k", "256k", "320k"];
    public IReadOnlyList<string> SampleRates { get; } = ["Auto", "44100", "48000", "88200", "96000", "192000"];
    public IReadOnlyList<string> BitDepths { get; } = ["Auto", "16", "24", "32"];
    public bool IsConvertPage => activeWorkspace == "Convert";
    public bool IsCutPage => activeWorkspace == "Cut";
    public bool IsCompressionPage => activeWorkspace == "Compress";
    public bool IsAboutPage => activeWorkspace == "About";
    public bool IsRemixPage => activeWorkspace == "Remix";
    public bool IsToolWorkspace => !IsAboutPage;
    public string ConvertTabState => IsConvertPage ? "Active" : "Inactive";
    public string CutTabState => IsCutPage ? "Active" : "Inactive";
    public string CompressionTabState => IsCompressionPage ? "Active" : "Inactive";
    public string AboutTabState => IsAboutPage ? "Active" : "Inactive";
    public string RemixTabState => IsRemixPage ? "Active" : "Inactive";
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
    public ObservableCollection<RemixEffectViewModel> RemixEffects { get; } = [];
    public ObservableCollection<MetadataTagViewModel> RemixCustomTags { get; } = [];
    public IReadOnlyList<RemixEffectKind> RemixEffectKinds { get; } = Enum.GetValues<RemixEffectKind>();
    public IReadOnlyList<string> RemixInspectorTabs { get; } = ["Export", "Metadata"];
    public IReadOnlyList<string> RemixCoverActions { get; } = ["Keep", "Replace", "Remove"];
    public string RemixSourcePath { get => remixSourcePath; set { if (Set(ref remixSourcePath, value)) RaiseCommands(); } }
    public double RemixDurationSeconds { get => remixDurationSeconds; private set { if (Set(ref remixDurationSeconds, value)) { Raise(nameof(RemixOutputDuration)); RaiseCommands(); } } }
    public double RemixOutputDuration
    {
        get
        {
            if (RemixDurationSeconds <= 0) return 0;
            var rate = BuildRemixRack().OfType<TempoPitchEffect>().FirstOrDefault(effect => effect.Enabled)?.Rate ?? 1;
            return double.IsFinite(rate) && rate > 0 ? RemixDurationSeconds / rate : RemixDurationSeconds;
        }
    }
    public double[] RemixWaveformPeaks { get => remixWaveformPeaks; private set => Set(ref remixWaveformPeaks, value); }
    public double RemixPreviewStartSeconds { get => remixPreviewStartSeconds; set { if (Set(ref remixPreviewStartSeconds, Math.Clamp(value, 0, Math.Max(0, RemixDurationSeconds - 1)))) Raise(nameof(RemixPreviewEndSeconds)); } }
    public double RemixPreviewEndSeconds { get => Math.Min(RemixDurationSeconds, RemixPreviewStartSeconds + 20); set => RemixPreviewStartSeconds = Math.Max(0, value - 20); }
    public AudioFormat RemixFormat { get => remixFormat; set { if (Set(ref remixFormat, value)) { Raise(nameof(IsRemixLossy)); Raise(nameof(IsRemixLossless)); } } }
    public bool IsRemixLossy => RemixFormat.IsLossy();
    public bool IsRemixLossless => !IsRemixLossy;
    public string RemixBitrate { get => remixBitrate; set => Set(ref remixBitrate, value); }
    public string RemixSampleRateOption { get => remixSampleRateOption; set => Set(ref remixSampleRateOption, value); }
    public string RemixBitDepth { get => remixBitDepth; set => Set(ref remixBitDepth, value); }
    public string RemixOutputFolder { get => remixOutputFolder; set => Set(ref remixOutputFolder, value); }
    public string RemixSuffix { get => remixSuffix; set => Set(ref remixSuffix, value); }
    public bool RemixOverwrite { get => remixOverwrite; set => Set(ref remixOverwrite, value); }
    public RemixEffectKind SelectedRemixEffectKind { get => selectedRemixEffectKind; set => Set(ref selectedRemixEffectKind, value); }
    public string RemixInspectorTab { get => remixInspectorTab; set { if (Set(ref remixInspectorTab, value)) { Raise(nameof(IsRemixExportTab)); Raise(nameof(IsRemixMetadataTab)); } } }
    public bool IsRemixExportTab => RemixInspectorTab == "Export";
    public bool IsRemixMetadataTab => RemixInspectorTab == "Metadata";
    public string RemixTitle { get => remixTitle; set => Set(ref remixTitle, value); }
    public string RemixArtist { get => remixArtist; set => Set(ref remixArtist, value); }
    public string RemixAlbum { get => remixAlbum; set => Set(ref remixAlbum, value); }
    public string RemixAlbumArtist { get => remixAlbumArtist; set => Set(ref remixAlbumArtist, value); }
    public string RemixGenre { get => remixGenre; set => Set(ref remixGenre, value); }
    public string RemixDate { get => remixDate; set => Set(ref remixDate, value); }
    public string RemixTrack { get => remixTrack; set => Set(ref remixTrack, value); }
    public string RemixDisc { get => remixDisc; set => Set(ref remixDisc, value); }
    public string RemixComment { get => remixComment; set => Set(ref remixComment, value); }
    public string RemixCoverAction { get => remixCoverAction; set => Set(ref remixCoverAction, value); }
    public string RemixCoverPath { get => remixCoverPath; set => Set(ref remixCoverPath, value); }
    public string RemixReport { get => remixReport; private set => Set(ref remixReport, value); }

    public RelayCommand ShowConvertCommand { get; }
    public RelayCommand ShowCutCommand { get; }
    public RelayCommand ShowCompressCommand { get; }
    public RelayCommand ShowAboutCommand { get; }
    public RelayCommand ShowRemixCommand { get; }
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
    public AsyncRelayCommand ChooseRemixFileCommand { get; }
    public RelayCommand<string> ApplyRemixPresetCommand { get; }
    public RelayCommand AddRemixEffectCommand { get; }
    public RelayCommand<RemixEffectViewModel> RemoveRemixEffectCommand { get; }
    public RelayCommand<RemixEffectViewModel> MoveRemixEffectUpCommand { get; }
    public RelayCommand<RemixEffectViewModel> MoveRemixEffectDownCommand { get; }
    public RelayCommand<string> ShowRemixInspectorCommand { get; }
    public RelayCommand ChooseRemixOutputCommand { get; }
    public RelayCommand ChooseRemixCoverCommand { get; }
    public RelayCommand RemoveRemixCoverCommand { get; }
    public RelayCommand AddMetadataTagCommand { get; }
    public RelayCommand<MetadataTagViewModel> RemoveMetadataTagCommand { get; }
    public AsyncRelayCommand PreviewRemixCommand { get; }
    public RelayCommand StopRemixCommand { get; }
    public AsyncRelayCommand ExportRemixCommand { get; }

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
        else if (IsRemixPage && File.Exists(path)) await LoadRemixSourceAsync(path);
    }

    private void SetWorkspace(string workspace)
    {
        if (!Set(ref activeWorkspace, workspace, nameof(activeWorkspace))) return;
        Raise(nameof(IsConvertPage));
        Raise(nameof(IsCutPage));
        Raise(nameof(IsCompressionPage));
        Raise(nameof(IsAboutPage));
        Raise(nameof(IsRemixPage));
        Raise(nameof(IsToolWorkspace));
        Raise(nameof(ConvertTabState));
        Raise(nameof(CutTabState));
        Raise(nameof(CompressionTabState));
        Raise(nameof(AboutTabState));
        Raise(nameof(RemixTabState));
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
            tools = FfmpegLocator.Find(); audio = new AudioProcessingService(tools); probe = new FfprobeService(tools.FfprobePath); remix = new RemixProcessingService(tools);
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
            tools = await installer.InstallAsync(progress: installProgress); audio = new AudioProcessingService(tools); probe = new FfprobeService(tools.FfprobePath); remix = new RemixProcessingService(tools); FfmpegMissing = false;
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

    private async Task ChooseRemixFileAsync()
    {
        var path = dialogs.ChooseAudioFile();
        if (path is not null) await LoadRemixSourceAsync(path);
    }

    private async Task LoadRemixSourceAsync(string path)
    {
        await RunBusyAsync(async () =>
        {
            EnsureFfmpeg();
            _ = AudioFormats.FromPath(path);
            Status = "Reading remix source and waveform…";
            var info = await probe!.ProbeAsync(path);
            var peaks = await audio!.ExtractWaveformAsync(path, 900);
            RemixSourcePath = path;
            RemixDurationSeconds = info.DurationSeconds;
            remixSampleRate = info.SampleRate ?? 44_100;
            remixSourceHasCover = info.HasCoverArt;
            RemixWaveformPeaks = peaks;
            RemixPreviewStartSeconds = 0;
            RemixFormat = AudioFormats.FromPath(path);
            RemixOutputFolder = Path.GetDirectoryName(path) ?? "";
            LoadRemixMetadata(info.Tags);
            UpdateOperationProgress(1, $"Remix source ready: {Path.GetFileName(path)}");
        });
    }

    private void ApplyRemixPreset(string presetName)
    {
        if (remixRackDirty && RemixEffects.Count > 0
            && !dialogs.Confirm("Applying a preset will replace the current effect rack. Continue?")) return;
        var preset = presetName switch
        {
            "Bass Boost" => RemixPreset.BassBoost,
            "Slowed + Reverb" => RemixPreset.SlowedReverb,
            "Sped Up + Reverb" => RemixPreset.SpedUpReverb,
            "Nightcore" => RemixPreset.Nightcore,
            "Deep Bass" => RemixPreset.DeepBass,
            "Vocal Boost" => RemixPreset.VocalBoost,
            "Dreamy Reverb" => RemixPreset.DreamyReverb,
            "Lo-Fi" => RemixPreset.LoFi,
            "Club" => RemixPreset.Club,
            "Acoustic Warmth" => RemixPreset.AcousticWarmth,
            "Telephone" => RemixPreset.Telephone,
            _ => RemixPreset.Custom,
        };
        RemixEffects.Clear();
        foreach (var effect in RemixPresetFactory.Create(preset))
            RemixEffects.Add(RemixEffectViewModel.From(effect, OnRemixRackChanged));
        remixRackDirty = false;
        Raise(nameof(RemixOutputDuration));
    }

    private void AddRemixEffect()
    {
        if (RemixEffects.Count >= 16) { dialogs.Error("A remix rack supports at most 16 effects."); return; }
        if (SelectedRemixEffectKind is RemixEffectKind.TempoPitch or RemixEffectKind.LoudnessNormalize or RemixEffectKind.FadeIn or RemixEffectKind.FadeOut
            && RemixEffects.Any(effect => effect.Kind == SelectedRemixEffectKind))
        {
            dialogs.Error($"{SelectedRemixEffectKind} can appear only once.");
            return;
        }
        var viewModel = new RemixEffectViewModel(SelectedRemixEffectKind, OnRemixRackChanged);
        var firstFade = RemixEffects.ToList().FindIndex(effect => effect.Kind is RemixEffectKind.FadeIn or RemixEffectKind.FadeOut);
        if (SelectedRemixEffectKind is RemixEffectKind.FadeIn or RemixEffectKind.FadeOut || firstFade < 0)
            RemixEffects.Add(viewModel);
        else
            RemixEffects.Insert(firstFade, viewModel);
        OnRemixRackChanged();
    }

    private void RemoveRemixEffect(RemixEffectViewModel effect)
    {
        RemixEffects.Remove(effect);
        OnRemixRackChanged();
    }

    private void MoveRemixEffect(RemixEffectViewModel effect, int offset)
    {
        var index = RemixEffects.IndexOf(effect);
        var destination = index + offset;
        if (index < 0 || destination < 0 || destination >= RemixEffects.Count) return;
        RemixEffects.Move(index, destination);
        OnRemixRackChanged();
    }

    private void OnRemixRackChanged()
    {
        remixRackDirty = true;
        Raise(nameof(RemixOutputDuration));
    }

    private async Task PreviewRemixAsync()
    {
        await RunBusyAsync(async () =>
        {
            var rack = BuildRemixRack();
            RemixRackValidator.Validate(rack, RemixDurationSeconds);
            previewPlayer.Stop();
            var progress = new Progress<double>(fraction => UpdateOperationProgress(fraction, "Rendering 20-second remix preview…"));
            var output = await remix!.RenderPreviewAsync(
                RemixSourcePath,
                rack,
                remixSampleRate,
                RemixDurationSeconds,
                RemixPreviewStartSeconds,
                progress);
            previewPlayer.Play(output);
            UpdateOperationProgress(1, "Playing remix preview.");
        });
    }

    private async Task ExportRemixAsync()
    {
        await RunBusyAsync(async () =>
        {
            var rack = BuildRemixRack();
            RemixRackValidator.Validate(rack, RemixDurationSeconds);
            var metadata = BuildRemixMetadata();
            RemixMetadataValidator.Validate(metadata);
            var hasRequestedCover = metadata.CoverAction == CoverArtAction.Replace
                || metadata.CoverAction == CoverArtAction.Keep && remixSourceHasCover;
            if (hasRequestedCover
                && RemixFormat is not (AudioFormat.Mp3 or AudioFormat.M4a or AudioFormat.Flac)
                && !dialogs.Confirm("The selected output format does not support embedded cover art here. Export without the cover?"))
            {
                Status = "Remix export cancelled.";
                TimingText = "";
                return;
            }

            var directory = string.IsNullOrWhiteSpace(RemixOutputFolder)
                ? Path.GetDirectoryName(RemixSourcePath)!
                : RemixOutputFolder;
            var output = OutputPathBuilder.Build(RemixSourcePath, directory, RemixFormat, RemixSuffix);
            var encoding = CreateRemixEncoding();
            var progress = new Progress<double>(fraction => UpdateOperationProgress(fraction, "Exporting remix…"));
            await remix!.ExportAsync(
                RemixSourcePath,
                output,
                rack,
                encoding,
                metadata,
                remixSampleRate,
                RemixDurationSeconds,
                remixSourceHasCover,
                progress);
            RemixReport = $"Export completed\nSource: {RemixSourcePath}\nOutput: {output}\nEffects: {rack.Count(effect => effect.Enabled)}\nDuration: {TimestampParser.Format(RemixOutputDuration)}";
            UpdateOperationProgress(1, $"Remix exported: {Path.GetFileName(output)}");
        });
    }

    private IReadOnlyList<RemixEffect> BuildRemixRack() => RemixEffects.Select(effect => effect.ToModel()).ToArray();

    private ConversionOptions CreateRemixEncoding() => new(
        RemixFormat,
        RemixFormat.IsLossy() ? RemixBitrate : null,
        RemixFormat.IsLossy() ? null : ParseOptionalInt(RemixSampleRateOption),
        RemixFormat.IsLossy() ? null : ParseOptionalInt(RemixBitDepth),
        PreserveMetadata: true,
        Overwrite: RemixOverwrite);

    private RemixMetadata BuildRemixMetadata()
    {
        var custom = RemixCustomTags.ToDictionary(tag => tag.Key, tag => tag.Value, StringComparer.OrdinalIgnoreCase);
        return new RemixMetadata(
            RemixTitle,
            RemixArtist,
            RemixAlbum,
            RemixAlbumArtist,
            RemixGenre,
            RemixDate,
            RemixTrack,
            RemixDisc,
            RemixComment,
            custom,
            Enum.Parse<CoverArtAction>(RemixCoverAction),
            string.IsNullOrWhiteSpace(RemixCoverPath) ? null : RemixCoverPath);
    }

    private void LoadRemixMetadata(IReadOnlyDictionary<string, string> tags)
    {
        string Get(params string[] keys) => keys.Select(key => tags.TryGetValue(key, out var value) ? value : null).FirstOrDefault(value => value is not null) ?? "";
        RemixTitle = Get("title");
        RemixArtist = Get("artist");
        RemixAlbum = Get("album");
        RemixAlbumArtist = Get("album_artist", "albumartist");
        RemixGenre = Get("genre");
        RemixDate = Get("date", "year");
        RemixTrack = Get("track");
        RemixDisc = Get("disc");
        RemixComment = Get("comment", "description");
        RemixCoverAction = "Keep";
        RemixCoverPath = "";
        var standard = new HashSet<string>(["title", "artist", "album", "album_artist", "albumartist", "genre", "date", "year", "track", "disc", "comment", "description"], StringComparer.OrdinalIgnoreCase);
        RemixCustomTags.Clear();
        foreach (var pair in tags.Where(pair => !standard.Contains(pair.Key)))
            RemixCustomTags.Add(new MetadataTagViewModel { Key = pair.Key, Value = pair.Value });
    }

    private void ChooseRemixCover()
    {
        var path = dialogs.ChooseCoverImage();
        if (path is null) return;
        RemixCoverPath = path;
        RemixCoverAction = "Replace";
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
    private void EnsureFfmpeg() { if (tools is null || audio is null || probe is null || remix is null) throw new FfmpegDependencyException("FFmpeg is required. Install it from this application first."); }

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

    private void RaiseCommands() { ConvertCommand.RaiseCanExecuteChanged(); CutCommand.RaiseCanExecuteChanged(); PreviewCommand.RaiseCanExecuteChanged(); InstallFfmpegCommand.RaiseCanExecuteChanged(); ChooseCutFileCommand.RaiseCanExecuteChanged(); ChooseCompressionFileCommand.RaiseCanExecuteChanged(); AnalyzeCompressionCommand.RaiseCanExecuteChanged(); CompressCommand.RaiseCanExecuteChanged(); ChooseRemixFileCommand.RaiseCanExecuteChanged(); PreviewRemixCommand.RaiseCanExecuteChanged(); ExportRemixCommand.RaiseCanExecuteChanged(); }
    public void Dispose()
    {
        previewPlayer.Dispose();
        remix?.CleanupTemporaryFiles();
    }
}
