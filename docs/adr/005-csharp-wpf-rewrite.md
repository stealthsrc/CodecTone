# ADR 005: C# WPF Rewrite

## Status

Accepted on 2026-08-25.

## Context

The Python/Tkinter implementation validates the product behavior but makes UI
composition, packaging, and continued customization increasingly expensive. The
application is permanently Windows-only, and performance, executable size, and
custom UI control are prioritized.

## Decision

- Rewrite the application in C# 12 with WPF on .NET 8.
- Separate Core, Infrastructure, and Desktop projects.
- Use MVVM without a third-party framework.
- Port one complete feature slice at a time and keep Python operational.
- Treat the Python tests and real FFmpeg readbacks as parity specifications.
- Publish both framework-dependent and self-contained `win-x64` artifacts.
- Keep FFmpeg outside the application and preserve the verified installer flow.

## Consequences

The rewrite duplicates behavior temporarily and requires Microsoft test packages
for the new test projects. Python is not removed until the C# artifact passes all
functional and performance checkpoints. The verified ZIP remains the rollback
path throughout the migration.
