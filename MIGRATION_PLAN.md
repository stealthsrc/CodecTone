# Implementation Plan: Python to C# WPF

## Objective

Replace the Python/Tkinter application with a Windows-only C# WPF application
while preserving every validated behavior. Keep the Python source and verified
backup until the C# artifact passes the parity checklist.

## Architecture

- `AudioConverter.Core`: models, validation, output naming, service contracts.
- `AudioConverter.Infrastructure`: FFmpeg, ffprobe, installer, waveform,
  playback, settings, and Windows paths.
- `AudioConverter.Desktop`: WPF, MVVM, WHITE/OLED themes, CONVERT/CUT AUDIO.
- MSTest projects for Core and Infrastructure behavior.
- .NET 8.0.420, C# 12, `win-x64`, no third-party UI or MVVM framework.

## Execution phases

1. Backup and baseline the Python implementation.
2. Scaffold the solution and establish Core contracts in TDD.
3. Port FFmpeg Infrastructure with mocked command/process/network tests.
4. Deliver CONVERT as a complete vertical slice.
5. Deliver CUT AUDIO, waveform, fades, preview, and fixed actions.
6. Publish framework-dependent and self-contained Windows artifacts.
7. Validate parity and switch launchers only after explicit final review.

## Checkpoints

- Foundation: Core builds independently and all Core tests pass.
- Infrastructure: real WAV-to-MP3 and trim-with-artwork smoke tests pass.
- CONVERT: six formats, batch continuation, metadata, and drag/drop match Python.
- CUT AUDIO: time selection, waveform, fades, preview, and layout match Python.
- Release: cold start <= 1.5 s, idle memory <= 150 MB, self-contained <= 80 MB.

## Safety rules

- Do not delete or move Python sources during migration.
- Do not overwrite the Python executable while it is running.
- Do not add UI packages; implement MVVM primitives locally.
- Keep FFmpeg external and verify the pinned archive before installation.
- Stop the migration if architecture logic enters WPF code-behind.
