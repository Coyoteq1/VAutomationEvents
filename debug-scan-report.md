# Debug scan report — 2025-12-24

## Summary ✅
- Quick scan found debug logging and debug-only operations across the repo. High-risk items include methods that unlock player progression or complete achievements.
- I implemented minimal safety edits: added clear warning headers to `*.cs.disabled` debug files and documented the debug-only system in `Services/SystemService.cs`.

---

## High-priority findings 🔥
- UI/PracticeArenaUI.cs
  - `VAuto.Plugin.Log?.LogDebug(...)` used to log VBlood unlock counts — normal debug logging (server-side) but worth keeping as `LogDebug`.

- Services/ProgressionService.cs.disabled
  - `OverwriteAllInMemory(...)` uses `SystemService.DebugEventsSystem` and calls:
    - `UnlockAllResearch(fromCharacter)`
    - `UnlockAllVBloods(fromCharacter)`
    - `CompleteAllAchievements(fromCharacter)`
  - **High risk**: these modify in-memory progression and can be abused if re-enabled.

- Services/SnapshotManagerService.cs.disabled
  - Similar `OverwriteAllInMemory(...)` with the same debug hooks above — **high risk**.

- Services/PlayerService.cs.disabled
  - `Plugin.Logger?.LogDebug(...)` used for cache refresh messaging — benign but debug-only.

- Services/SystemService.csoverwite the allvbloods for 30secs

  - Declares `DebugEventsSystem DebugEventsSystem` getter. This is the central debug system used by the risky methods above.

- VAutomationevents.csproj
  - Contains `<DebugType>embedded</DebugType>` and `<DebugSymbols>true</DebugSymbols>` — expected for debug builds, but confirm only desired configs include them in production CI.

- Tests
  - Test files use `_logger?.LogError` / `LogWarning` appropriately — not a concern.

---

## Actions taken (quick, safe) ✅
- Added header comments to:
  - `Services/ProgressionService.cs.disabled`
  - `Services/SnapshotManagerService.cs.disabled`
  These headers clearly warn that the files contain debug-only operations and must not be re-enabled or used in production without review and explicit opt-in.
- Added an XML doc comment to `Services/SystemService.cs`'s `DebugEventsSystem` property noting it is debug-only and should be used with caution.

---

## Recommended follow-ups (pick priority) 🔧
1. Add a gated config flag (e.g., `Plugin.Config.AllowDebugActions`) required for any runtime calls that mutate progression via `DebugEventsSystem`.
2. Add a CI check (lint or grep) to fail the build on `UnlockAll*` calls or `DebugEventsSystem` usage outside test directories unless explicitly approved.
3. Consider leaving `DebugSymbols` in debug builds but ensure `Release` artifacts are stripped as intended.
4. Add unit/integration tests for any gating logic introduced.

---

## Next steps I can perform (fast) ▶️
- Create a feature branch `chore/debug-scan-fixes` and commit the above edits (if you want me to handle git operations for you).  
- Implement a guard in the disabled files and/or SystemService that requires an explicit config flag before calling `UnlockAll*`.  
- Add a small CI lint rule to detect these patterns.

If you'd like, I can proceed to create the branch and commit these changes now (or just open a PR summary for you to review).