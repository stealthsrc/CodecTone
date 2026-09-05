# ADR 009: Album artwork extraction workspace

## Status

Accepted on 2026-08-30.

## Understanding summary

- Add an EXTRACT ARTWORK workspace for one audio file or a recursive folder.
- Extract one front cover per album and deduplicate repeated album entries.
- Preserve the embedded image by default or convert it to PNG, JPEG, or WebP.
- Name images `Artist - Album.ext` with Windows-safe normalization.
- Preserve dimensions by default and support an optional maximum dimension.
- Keep processing local, cancellable, transactional, and independent per file.

## Assumptions

- Supported audio sources remain MP3, FLAC, WAV, OGG, AAC, and M4A.
- The default destination is an `artwork` folder beside the selected source.
- Album artist falls back to artist, then the source directory; album falls back
  to the source directory, then the audio file name.
- A stream tagged as a front cover is preferred; the first attached picture is
  the fallback.
- An existing identical image is skipped. A different collision receives `_2`,
  `_3`, and so on.
- Thousands of tracks are supported without loading complete images into memory.
- Audio files are never modified and no online artwork lookup is performed.

## Decision

Use a dedicated ffprobe and FFmpeg pipeline. Extend probing with embedded image
stream index, codec, dimensions, disposition, and tags. Core creates a plan by
grouping normalized artist and album metadata. Infrastructure extracts the chosen
stream directly for Original or encodes a single frame for PNG, JPEG, or WebP.

Every image is written to a same-directory staged path, validated with ffprobe,
hashed, and moved atomically. Image hashes are streamed. Batch failures and files
without artwork are reported and do not stop later albums.

## Error handling

- Missing artwork is reported as skipped, not failed.
- Invalid image streams, unsupported original codecs, and failed conversions are
  isolated to the affected album.
- Cancellation kills the FFmpeg process tree and removes staged files.
- Existing identical files are skipped; non-identical names receive a suffix.
- Inaccessible folders are skipped by the existing safe recursive discovery.

## Test plan

- Core tests for album grouping, fallbacks, safe names, and collision names.
- Parser tests for multiple image streams and front-cover selection.
- Command tests for copy, PNG/JPEG/WebP conversion, and bounded scaling.
- Service tests for transactional extraction, hashing, cancellation, and errors.
- Real FFmpeg smoke tests with repeated albums, missing covers, and conversions.
- Full regression suite, strict Release build, and both Windows GUI publications.

## Decision log

- File and recursive folder input selected over single-file only.
- Original plus optional conversion selected over forced conversion.
- One cover per album selected over one cover per track.
- Front cover with first-image fallback selected over extracting every image.
- `Artist - Album` selected over templates for the first version.
- Original dimensions plus optional maximum selected over forced square resizing.
- Hash skip and numbered collision selected over prompts or automatic overwrite.
- ffprobe and FFmpeg selected over a new metadata library or WPF image conversion.
