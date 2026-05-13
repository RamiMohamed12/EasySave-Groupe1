# Livrable 3 Runtime Control

This document captures the runtime invariants for the Livrable 3 workstream.

## Global scheduling rules

- File transfer scheduling is global to the application, not local to a single job.
- Priority files must be transferred before any non-priority file can start.
- Large-file throttling is global: no more than one transfer above the configured threshold may run at the same time.
- Small files may run in parallel if the priority rule is already satisfied.

## Runtime control rules

- Each backup job can receive `Pause`, `Resume`, and `Stop` requests.
- A paused job keeps its state and can continue from remaining work.
- A stopped job halts immediately and does not auto-resume.
- Business software detection pauses a job automatically instead of stopping it.
- Auto-paused jobs resume automatically once the blocking process disappears, unless a manual stop was requested.

## Persistence rules

- `jobs.json`, `state.json`, and `backup-history.json` must use atomic writes.
- Critical runtime files must be protected against concurrent access.
- Existing JSON formats remain backward compatible.

