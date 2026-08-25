# Python Baseline

Date: 2026-08-25

Artifact: `dist/AudioConverter-scroll-update.exe`

## Source state

- Backup entries: 19
- Backup ZIP: 285,464 bytes
- Backup SHA-256: `5AC24A561F7B657A44BDAA9EBE9DBD87EDACB5D317863B802681EAD00F0D8E39`
- Unit tests: 46 passing

## Runtime measurements

Five cold GUI starts on the current Windows machine:

| Run | Startup | Working set |
| --- | ---: | ---: |
| 1 | 1,401 ms | 55,963,648 bytes |
| 2 | 1,137 ms | 56,385,536 bytes |
| 3 | 997 ms | 56,082,432 bytes |
| 4 | 970 ms | 56,303,616 bytes |
| 5 | 997 ms | 56,172,544 bytes |

- Mean startup: 1,100.4 ms
- Mean combined working set: 53.6 MiB
- Executable size: 12,907,855 bytes

## Synthetic operation measurements

- Windowed EXE WAV-to-MP3 conversion: 2,035 ms
- In-process 0.4-second FLAC trim with fades: 184 ms

These synthetic operation figures mainly verify the measurement harness. Release
comparison must reuse the same inputs and commands.
