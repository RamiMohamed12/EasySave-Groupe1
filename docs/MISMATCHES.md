# EasySave: Mismatches Between Code and Docs

This file explains, in simple language, what is still different between the current code and the project specification.

## 1. Log and state file location is different

What the docs say:

- The daily log file should be created in the same folder as the executable.
- The project should avoid hardcoded locations like `C:\temp`.

What the code does now:

- The code creates a `BackupState` folder.
- On Windows, it uses `C:\BackupState`.
- On Linux, it uses `/home/<user>/BackupState`.

Why this is a mismatch:

- This is not the same as "same folder as the executable".
- It is also a fixed OS-based location.

## 2. Multilingual support is missing

What the docs say:

- The UI must support at least French and English.

What the code does now:

- All console messages are only in English.
- There is no language choice.
- There is no translation system or resource file.

Why this is a mismatch:

- The application does not yet support two languages.

## 3. `state.json` does not fully represent all configured jobs

What the docs say:

- `state.json` should reflect the state of all backup jobs.

What the code does now:

- It writes the state only for jobs that were actually used during the current run.
- Jobs that exist in `jobs.json` but were not launched may not appear in `state.json`.

Why this is a mismatch:

- The file should describe all jobs, not only the ones just executed.

## 4. The default sample job paths are not cross-platform

What the docs say:

- The app should work with different machines and different kinds of paths.

What the code does now:

- The default generated `jobs.json` uses Linux-style sample paths like `/home/user/source1`.

Why this is a mismatch:

- These sample paths are not correct for Windows.
- The generated configuration is not adapted to the machine automatically.

## What already matches the docs

These parts are already close to the specification:

- The app supports `1-3` and `1;3` job selection.
- The log system is separated into `EasyLog`.
- A daily JSON log file is created.
- `state.json` is written in JSON.
- Full and differential backup logic both exist.
