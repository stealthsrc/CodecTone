# ADR 008: Adaptive categorized remix presets

## Status

Accepted on 2026-08-25.

## Understanding summary

- Organize Remix presets into six categories instead of one flat list.
- Add Light, Medium, and Strong intensity without hiding the editable rack.
- Analyze each loaded song locally and adapt preset values to its audio profile.
- Improve consistency across different titles without classifying genre or using AI.
- Add bounded mastering effects for dynamics, filtering, stereo width, and limiting.
- Keep analysis responsive, cached, private, and safe when FFmpeg cannot provide a metric.

## Assumptions

- FFmpeg and ffprobe remain the only audio-processing dependencies.
- Analysis runs in the background and should normally finish within five seconds.
- The cache is local JSON keyed by normalized path, file size, and modification time.
- Adaptation changes preset parameters only inside documented safe limits.
- A preset remains usable with its static defaults when analysis fails or is cancelled.
- No genre classifier, machine-learning model, network request, or telemetry is added.

## Decision

Use deterministic, bounded rules driven by a full-song `AudioAnalysis`:

- integrated loudness and loudness range;
- peak and crest factor;
- low-, mid-, and high-band energy;
- stereo width;
- duration, sample rate, and channel count.

The analyzer executes independent FFmpeg measurements concurrently, combines their
results, and caches the complete result under
`%LOCALAPPDATA%\AudioConverter\remix\analysis`. Preset adaptation starts from a
static reference rack, applies the selected intensity, and then makes small metric-
based corrections. Each correction is clamped to the effect validator's safe range.

Intensity scales creative changes around the neutral value: Light uses about 65%,
Medium 100%, and Strong up to 125%. Loudness targets and safety processors are not
blindly amplified. Adaptation reduces bass boosts for bass-heavy sources, reduces
reverb for dense or narrow-dynamic sources, moderates brightness for treble-heavy
sources, and avoids widening already-wide material. The UI exposes a short
explanation of the active adjustments and an `ADAPTIVE` status.

## Preset categories

- Speed & Pitch: Slowed + Reverb, Deep Slowed, Sped Up + Reverb, Nightcore, Half-Time.
- Bass & Punch: Warm Bass, Bass Boost, Deep Bass, Sub Focus, Club Punch.
- Atmosphere: Light Room, Dreamy Reverb, Wide Hall, Ambient Wash, Echo Space.
- Vocal & Clarity: Vocal Boost, Vocal Presence, Soft Vocal, De-Mud, Clear Mix.
- Color & Texture: Lo-Fi, Telephone, Radio, Vintage Warm, Dark Tone, Bright Tone.
- Mastering: Clean Master, Loud Master, Dynamic Master, Wide Master,
  Streaming -14 LUFS, Club Master -10 LUFS.

New rack effects are Compressor, Stereo Width, High-Pass, Low-Pass, and Soft
Limiter. FFmpeg maps them to `acompressor`, `stereotools`, `highpass`, `lowpass`,
and `alimiter` respectively.

## Data flow and failure handling

`Load source → probe/waveform + cached analysis → category/intensity → adaptive
preset → editable rack → preview/export`

- Analysis is cancellable when another source is selected or the window closes.
- A corrupt or partial cache entry is ignored and replaced after a successful run.
- Missing metrics use conservative neutral values and never block preview or export.
- The rack validator rejects invalid effect values and unsafe ordering.
- Output limiting prevents adaptive gain changes from clipping the rendered file.

## Test plan

- Core tests for categories, intensity scaling, bounded rules, fallback, and explanations.
- Core validation tests for every new effect and rack ordering.
- Infrastructure tests for FFmpeg filter fragments and analysis parsing.
- Cache tests for hits, invalidation, corrupt entries, and cancellation.
- Desktop tests where practical for category filtering and analysis state transitions.
- Real FFmpeg smoke tests on quiet, loud, bass-heavy, bright, mono, and stereo sources.
- Rebuild and launch both framework-dependent and self-contained Windows executables.

## Decision log

- Bounded rules were selected over fixed tables and local ML for predictability and size.
- Full-song analysis was selected over preview-only analysis for stable measurements.
- Background cached analysis was selected over blocking analysis on every preset click.
- Category tabs were selected over a long grouped list or preset search.
- One global intensity was selected over separate strength controls per effect.
- Mastering effects were selected; advanced spatial and 8D processing were excluded.
- Safe static fallback was selected so analysis availability never blocks remixing.
