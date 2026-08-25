# ADR 004: Audio Preview and Scrollable Cut View

## Status

Accepted on 2026-08-24.

## Context

The CUT AUDIO form can exceed the usable height of a 768-pixel display, pushing
its primary action below the visible window. Users also need to hear the exact
selection and fades before writing the final output.

## Decision

- Cap the main window height against the detected screen height.
- Place CUT AUDIO inside a vertically scrollable canvas.
- Use the full viewport width for the waveform.
- Arrange selection, fades, and output settings as compact horizontal bands.
- Hide the CUT scrollbar when all controls fit in the viewport.
- Use a slim arrowless scrollbar with an accent hover state.
- Bind mouse-wheel scrolling to every CUT child widget and normalize small
  precision-touchpad deltas so they never round down to zero.
- Keep Preview, Stop, progress, status, and Cut audio outside that canvas.
- Keep the operation report compact and independently scrollable.
- Render the current selection to a reusable PCM WAV under
  `%LOCALAPPDATA%\AudioConverter\preview\selection.wav`.
- Apply the same `atrim`, timestamp reset, fade in, and fade out filters used by
  the final cut.
- Play and stop the WAV asynchronously through Python's Windows sound API.
- Stop playback when the source, selection, workspace, or application changes.

## Consequences

Preview rendering takes time proportional to the selected audio and temporarily
uses uncompressed WAV storage. Playback supports Play and Stop only; pause and
timeline scrubbing are not provided.
