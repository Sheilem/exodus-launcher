# Suggestion: a real Exodus launcher (first version)

Hey — I put together a working **first version** of a new launcher for Exodus and
wanted to run it by you. No pressure at all; it's just a prototype to show the idea,
and the full source is included so you can read/build it yourself (nothing to
blindly trust).

## Why
Right now the launcher doesn't patch — any client change means players re-download.
A manifest-based patcher means **you push an update and players get only the changed
files in seconds.** That's the biggest quality-of-life win for both sides.

## What this first version already does
- **Auto-patch:** manifest + SHA-256 diff, downloads only changed files, verifies each one, then launches `Exodus.exe`. Re-launch is instant thanks to a local cache.
- **Server status + uptime** right in the launcher (online/offline, uptime, players online).
- **News + Patch Notes** tabs for announcements and version changelogs.
- **In-launcher settings:** a GUI for `config.ini` (resolution, windowed, skip-fade, WASD, etc.) so players stop hand-editing the file.
- Classic MapleStory look.

## What it'd need server-side (small)
1. Run one PowerShell script against a clean client build → it produces `manifest.json` + a mirror of the files.
2. Drop those on any static host / object storage (R2/Vercel/S3/nginx all work).
3. Keep small `status.json`, `news.json`, `patchnotes.json` updated (can be automated).

No launcher rebuild needed to push a game update — just re-run the script and sync.

## It's safe to check
- **Full C# source is in the repo** — read it or build it yourself with `dotnet build`. No obfuscation, no external dependencies.
- WPF / .NET 8, single self-contained `.exe` once built.
- Build/deploy steps are in `README.md`.

This is a first pass — happy to hand over the source, change the design, drop or add
features, or wire it to whatever hosting you prefer. Just thought it could be a nice
upgrade for the server.
