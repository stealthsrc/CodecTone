# Changelog

## 1.1.0 - 2026-08-25

### Added

- Added the single-track Remix Audio workspace with a movable 20-second preview, editable effect rack, metadata editor, and cover-art controls.
- Added 32 remix presets organized into Speed & Pitch, Bass & Punch, Atmosphere, Vocal & Clarity, Color & Texture, and Mastering categories.
- Added Light, Medium, and Strong preset intensity levels.
- Added local full-song analysis for loudness, dynamics, frequency balance, and stereo width, with an invalidating JSON cache and static fallback.
- Added Compressor, Stereo Width, High-Pass, Low-Pass, and Soft Limiter effects.
- Added cancellation controls for the GUI and Ctrl+C cancellation for the CLI.

### Changed

- Presets now adapt bounded parameters to the loaded song while keeping the generated rack editable.
- Waveform extraction now bounds decoded samples for long audio files.
- Recursive compression skips inaccessible folders and directory links instead of stopping the batch.
- Managed FFmpeg installations take precedence over PATH executables.

### Fixed

- Replaced metallic echo-based reverb with convolution reverb using a verified embedded impulse response.
- Prevented failed conversions from truncating or replacing an existing destination.
- Added ffprobe validation and atomic destination replacement for conversion, compression, and cutting.
- Fixed false successful results when a destination already existed.
- Stopped FFmpeg process trees when an operation is cancelled or the application closes.
- Prevented format, suffix, and output settings from changing during an active batch.
- Serialized concurrent settings and remix-analysis cache writes.
- Fixed adaptive analysis and loudness normalization for mono and silent audio.
- Preserved batch processing after individual corrupt-file failures without leaving partial outputs.
