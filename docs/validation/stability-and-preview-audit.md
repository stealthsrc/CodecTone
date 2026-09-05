# Stability and preview audit

## Verified corrections

- Remix rejects identical source/destination paths before starting FFmpeg.
- Audio and artwork check cancellation immediately before final publication.
- Window closure cancels work and awaits operations and analyses before cleanup.
- A second close is dispatched after the original Closing event returns.
- Preview applies the rack on the full timeline, then selects the tempo-adjusted interval.
- Distortion rejects fractional oversampling and preserves small thresholds.
- Album artwork ranks front covers and pixel area across tracks and retries alternate candidates.
- Waveform streams native-rate interleaved PCM into bounded peak buckets, preserving anti-phase channels.
- Folder traversal reports skipped entries, accepts cancellation, and runs off the GUI dispatcher.
- Reports retain completed entries after cancellation; diagnostics rotate locally at about 1 MiB total.

## User-facing changes

- PREVIEW ORIGINAL and PREVIEW REMIX process the same source interval.
- Preview volume defaults to 25% and affects the next rendering, never export or Windows volume.
- PREVIEW COVER shows a thumbnail with original dimensions, codec and source path.
- SAVE REPORT exports the current report; About opens the local diagnostic log.

## Validation

Run dotnet test AudioConverter.sln -c Release on Windows with FFmpeg available.
Core, Infrastructure and Desktop tests cover cancellation, source protection, locked destinations, artwork fallback, waveform peaks, rack confirmation and shutdown.
A real PCM comparison checks preview fades against full export at the beginning, middle and end of a tempo-adjusted fixture.
Desktop layout tests render OLED/WHITE Remix and Artwork at 900 x 650 and save layout-*.png beside the test assembly.

## Limits

- No destructive physical disk-full test: write failures and locked destinations are tested.
- Full-timeline preview processing takes longer for late excerpts.
- Preview normalization is single-pass; final two-pass normalization may differ.
- Listening-volume changes apply on the next Preview click.
- Peak measurements do not establish resemblance to a reference song.
- Published v1.1.0 assets and tags are unchanged.
