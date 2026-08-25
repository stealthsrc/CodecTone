# ADR 001: Managed FFmpeg Runtime

## Status

Accepted on 2026-08-24.

## Context

The converter requires both `ffmpeg` and `ffprobe`. Requiring a separate system
installation creates a setup barrier for Windows users. Automatically executing
an unverified archive would introduce a software supply-chain risk.

## Decision

Use a system FFmpeg installation when both tools are on `PATH`. Otherwise, the
Windows GUI offers an explicit one-time installation of FFmpeg 9.0.1 Essentials
inside `runtime/ffmpeg`. The CLI exposes the same operation through
`--install-ffmpeg`.

The managed artifact is pinned to:

- Source: `https://www.gyan.dev/ffmpeg/builds/packages/ffmpeg-9.0.1-essentials_build.zip`
- SHA-256: `fec81ae03971d9dd4be3ebe02e263bd2ec1d789483f931bdba5f5715e65da2e9`
- License: GPLv3

## Security controls

- Require HTTPS and reject redirects outside `www.gyan.dev`.
- Limit the archive to 160 MiB and each executable to 300 MiB.
- Verify the pinned SHA-256 before opening the ZIP.
- Select only one `bin/ffmpeg.exe` and one `bin/ffprobe.exe` entry.
- Reject duplicate entries, symbolic links, invalid sizes, and non-PE files.
- Run `-version` through absolute paths without a shell before activation.
- Rename the validated staging directory into place on the same filesystem.
- Never modify the system `PATH` and never request administrator rights.

## Consequences

Conversions remain offline. The installer performs one user-initiated network
download of approximately 106 MB. Updating FFmpeg requires changing the pinned
version, URL, checksum, tests, and this decision record.
