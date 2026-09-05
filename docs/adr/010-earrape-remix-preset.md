# ADR 010: EARRAPE remix preset

## Status

Accepted on 2026-08-30.

## Understanding summary

- Add EARRAPE to the Color & Texture remix category.
- Generate deliberately extreme distortion and perceived loudness.
- Ignore the global Light, Medium, and Strong intensity selection.
- Keep the generated rack visible and editable.
- Confirm every preset activation and show a persistent hearing warning.
- Cap the encoded waveform at -0.1 dBFS without controlling Windows volume.

## Assumptions

- Preview and export use the same effect rack.
- The preset is never automatically adapted to source analysis.
- The warning recommends lowering playback volume before listening.
- Metadata, cover art, cancellation, and transactional export remain unchanged.
- FFmpeg remains the only processing dependency.

## Decision

Keep editable Distortion and Bit Crusher rack modules, but do not use Bit Crusher
inside the fixed EARRAPE preset. The preset uses bass and mid reinforcement, high
input drive into wide-band hard clipping, aggressive compression and makeup gain,
a controlled final gain stage, and a final -0.1 dBFS limiter.

The distortion filter maps to `volume` followed by `asoftclip=type=hard` with
oversampling. Bit Crusher still maps to `acrusher` for custom racks. The final
limiter remains last.

## Safety and error handling

- Applying EARRAPE always opens a hearing-risk confirmation dialog.
- Cancelling the dialog leaves the current rack unchanged.
- A red warning remains visible while Distortion or Bit Crusher is enabled.
- The application does not alter system volume and cannot guarantee safe headphone
  or speaker output levels.
- Invalid module values block preview and export through rack validation.

## Test plan

- Core tests for category placement, fixed intensity, rack order, and bounds.
- Infrastructure tests for hard clipping, bit crushing, and final limiter filters.
- WPF build validation for the confirmation and persistent warning binding.
- Real preview smoke test with peak and loudness readback.
- Full regression suite and Windows executable rebuild.

## Decision log

- Persistent warning plus per-activation confirmation selected over a one-time notice.
- A -0.1 dBFS final ceiling selected over uncontrolled digital clipping.
- Color & Texture selected over Mastering or a new category.
- Fixed maximum processing selected over global intensity scaling.
- Editable rack modules selected over an opaque one-off FFmpeg filter string.
