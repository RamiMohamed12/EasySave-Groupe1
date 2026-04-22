# EasySave: Mismatch Status

All mismatches listed previously in this file have now been fixed in the codebase.

## Resolved items

1. Log and state files now use the executable directory

- `RuntimeStoragePaths` stores the daily log file, `state.json`, `jobs.json`, and related JSON files beside the executable.
- There is no hardcoded OS-specific storage path.

2. The console UI now supports English and French

- User-facing messages are translated through a dedicated text service.
- The app selects French automatically when the current UI culture is French and falls back to English otherwise.

3. `state.json` now covers all configured jobs

- `StateService` synchronizes `state.json` with every job from `jobs.json`.
- Jobs that are configured but not currently running still appear as inactive entries.

4. Default sample paths now adapt to the current machine

- The generated sample jobs use a machine-safe base path under the current user's documents folder.
- This keeps the sample configuration valid on both Windows and other supported environments.
