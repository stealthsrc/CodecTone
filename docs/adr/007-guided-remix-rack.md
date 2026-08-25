# ADR 007: Guided single-track remix rack

## Status

Accepted on 2026-08-25.

## Understanding summary

- Add REMIX AUDIO for one song at a time; folders and multi-track mixing are excluded.
- Combine presets, advanced controls, and a fully ordered effect rack.
- Couple speed and pitch for classic slowed and sped-up effects.
- Render a selectable 20-second local preview before export.
- Export to MP3, FLAC, WAV, OGG, AAC, or M4A.
- Provide complete standard/custom metadata and cover-art editing.
- Keep sources untouched and use validated staged output.

## Assumptions

- FFmpeg and ffprobe remain the only audio-processing dependencies.
- Preview rendering should normally complete in about three seconds on a current PC.
- The UI remains responsive and exposes progress and ETA for preview and export.
- Temporary previews and staged exports are cleaned on success, failure, and exit.
- No cloud storage, telemetry, VST, MIDI, microphone, or folder batch is added.

## Decision

Use a guided rack layout:

1. One source picker and full-width waveform.
2. A movable 20-second preview selection.
3. Presets: Bass Boost, Slowed + Reverb, Sped Up + Reverb, Nightcore,
   Deep Bass, Vocal Boost, Dreamy Reverb, Lo-Fi, Club, Acoustic Warmth,
   Telephone, and Custom.
4. A 60/40 workspace with the ordered rack and Export/Metadata inspector.
5. Fixed Preview 20s, Stop, and Export Remix actions.

Effects are Tempo/Pitch, Bass, three-band Equalizer, Reverb, Echo, Volume,
Fade In, Fade Out, and Loudness Normalize. The rack is limited to 16 effects.
Tempo/Pitch, Normalize, and each fade are unique. Normalize follows gain and
tone effects; fades remain at the end.

Preset values:

- Bass Boost: +8 dB at 90 Hz and -2 dB output volume.
- Slowed + Reverb: 0.85x, 28% wet mix, 2.4 s decay.
- Sped Up + Reverb: 1.18x, 18% wet mix, 1.6 s decay.
- Custom: empty rack.

The default export format matches the source and uses `_remix`. Source metadata
is copied before user overrides. Custom tag keys accept letters, digits, `.`,
`_`, and `-`. PNG, JPEG, and WebP covers up to 20 MiB are accepted. Cover art
is emitted only for MP3, M4A, and FLAC.

## Processing flow

`Load → ffprobe/waveform → preset/rack → 20 s preview → metadata → staged
export → ffprobe validation → atomic final move`

Tempo/pitch uses `asetrate` and high-quality `aresample`. Bass and EQ use their native
FFmpeg filters. Reverb uses `afir` convolution with an embedded, filtered hall
impulse response, explicit dry/wet mixing, and output limiting; Echo remains a
distinct delay effect. Preview normalization is single-pass; export
normalization uses analysis before the final pass.

## Error handling

- Invalid parameters block preview/export and identify the affected module.
- Unsupported cover output is explained before export.
- Failed validation never replaces an existing destination.
- The current rack and metadata remain available after any error.

## Test plan

- Core validation, ordering, presets, duration, tags, and output naming.
- Infrastructure filter graphs, metadata arguments, covers, staging, and cleanup.
- Preview selection and full export progress.
- Real FFmpeg smoke tests for all three presets and all destination formats.

## Decision log

- Guided rack selected over separate simple/advanced modes and node graphs.
- Full metadata editor selected over standard fields only.
- Classic coupled speed/pitch selected over pitch preservation.
- User-selectable export format selected over source-only or MP3-only output.
- Twenty-second preview selected over full-song preview.
