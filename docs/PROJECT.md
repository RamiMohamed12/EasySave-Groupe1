Based on the spec, here is what EasySave v1.0 does:

---

**Core concept**

It is a console application that manages up to 5 backup jobs. Each job has a name, a source directory, a target directory, and a type (full or differential).

---

**Running it**

It can be launched from the terminal with arguments to control which jobs run:

- `EasySave.exe 1-3` runs jobs 1 through 3 sequentially
- `EasySave.exe 1;3` runs jobs 1 and 3 specifically

---

**Backup types**

- Full: copies everything from source to target
- Differential: copies only files that have changed since the last full backup

Source and target directories can be local disks, external drives, or network drives.

---

**Two output files it maintains in real time**

1. A daily log file (named by date, e.g. `2020-12-17.json`) — records every file transfer with timestamp, backup name, source path, destination path, file size, and transfer time in ms
2. A state file (`state.json`) — reflects the current progress of all jobs at any given moment: how many files remain, sizes, current file being copied, etc.

Both files are JSON with newlines between elements, and must not be placed in hardcoded paths like `C:\temp`.

---

**The log feature is a separate DLL**

The daily log writing must be extracted into a separate class library called `EasyLog.dll`. This is intentional — it will be reused by future projects, so it must stay backward compatible.

---

**Multilingual**

The UI must support at minimum French and English.

