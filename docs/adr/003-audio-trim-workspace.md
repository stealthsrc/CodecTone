# ADR 003: Audio Trim Workspace

## Status

Accepted on 2026-08-24.

## Context

The application originally exposed one conversion form. Audio cutting needs a
different interaction: one source file, a visible timeline, precise start/end
times, and optional fades. Adding these controls to the conversion form would
mix batch and single-file workflows.

## Decision

- Expose two top-level workspaces: `CONVERT` and `CUT AUDIO`.
- Keep conversion batch behavior unchanged.
- Limit cutting to one file and visualize its locally decoded mono waveform.
- Allow draggable start/end handles and exact `HH:MM:SS.mmm` fields.
- Re-encode the selected range for accurate boundaries and audio fades.
- Apply `afade` from the start of the output and from
  `selection_duration - fade_out_duration`.
- Preserve compatible metadata and cover art through the existing mapping.
- Use the selected destination format; default lossy bitrate is 192 kbit/s.
- Share the existing progress bar, report, theme, drag-and-drop, and FFmpeg
  dependency flow.

## Consequences

Waveform generation decodes the selected audio to a low-rate mono PCM stream.
Long files can take several seconds to prepare. Playback preview is not included
because the managed runtime does not install or expose `ffplay`.
