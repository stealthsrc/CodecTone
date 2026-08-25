using Microsoft.Win32;
using System.Windows;

namespace AudioConverter.Desktop.Services;

public sealed class DialogService
{
    private const string AudioFilter = "Audio files|*.mp3;*.flac;*.wav;*.ogg;*.aac;*.m4a|All files|*.*";

    public string? ChooseAudioFile()
    {
        var dialog = new OpenFileDialog { Filter = AudioFilter, CheckFileExists = true, Multiselect = false };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? ChooseFolder(string? initialDirectory = null)
    {
        var dialog = new OpenFolderDialog { InitialDirectory = initialDirectory ?? "", Multiselect = false };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    public void Error(string message) => MessageBox.Show(message, "CodecTone", MessageBoxButton.OK, MessageBoxImage.Error);

    public bool Confirm(string message) => MessageBox.Show(
        message,
        "CodecTone",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning) == MessageBoxResult.Yes;
}
