using System.Runtime.InteropServices;

namespace AudioConverter.Infrastructure.Audio;

public sealed class WavePreviewPlayer : IDisposable
{
    private const uint SoundAsync = 0x0001;
    private const uint SoundFileName = 0x00020000;
    private const uint SoundNoDefault = 0x0002;

    public void Play(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Aperçu audio introuvable.", path);
        Stop();
        if (!PlaySound(Path.GetFullPath(path), nint.Zero, SoundAsync | SoundFileName | SoundNoDefault))
        {
            throw new InvalidOperationException("Windows n’a pas pu lire l’aperçu WAV.");
        }
    }

    public void Stop()
    {
        PlaySound(null, nint.Zero, 0);
    }

    public void Dispose() => Stop();

    [DllImport("winmm.dll", EntryPoint = "PlaySoundW", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PlaySound(string? sound, nint module, uint flags);
}
