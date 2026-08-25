# ADR 002: OLED Theme and Windows Packaging

## Status

Accepted on 2026-08-24.

## Context

The source application requires Python and previously exposed one light palette.
A standalone Windows executable needs embedded visual assets and persistent data
paths that do not point into PyInstaller's temporary extraction directory.

## Decision

- Provide `white` and `oled` palettes. OLED uses `#000000` as its base surface.
- Store the selected theme in `%LOCALAPPDATA%\AudioConverter\settings.json`.
- Store managed FFmpeg under `%LOCALAPPDATA%\AudioConverter\runtime\ffmpeg`.
- Resolve read-only assets from PyInstaller's `_MEIPASS` directory when frozen.
- Build one windowed `AudioConverter.exe` with PyInstaller `onefile`.
- Embed the generated ICO in the PE executable and bundle the PNG for Tkinter.
- Keep FFmpeg outside the executable and install it through the verified flow in
  ADR 001 when needed.

## Consequences

The executable starts without a console and needs no Python installation on the
target computer. PyInstaller extracts application resources temporarily at
startup. User preferences and FFmpeg remain persistent because they live under
`%LOCALAPPDATA%`, not inside that temporary directory.
