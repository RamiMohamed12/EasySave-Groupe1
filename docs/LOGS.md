## The application must on real time data save files into JSON format, with these minimal information :

1. Hour and date
2. Name of the backup
3. UNC path of the source file
4. UNC path of the destination file
5. Size of the file in bytes
6. Time taken to transfer the file in milliseconds
7. Time taken to encrypt the file in milliseconds (`0` means no encryption, a positive value is the CryptoSoft duration, and a negative value is a CryptoSoft error code)

The file must be named like this: `2020-12-17.json` and must be created in the same directory as the executable. Each line of the file must be a JSON object with the above information, and there must be a newline between each JSON object.

A separate class library called `EasyLog.dll` must be used to handle the logging functionality. It must be designed to be reusable in future projects while maintaining backward compatibility.

## File state in real time

The application must also save data in real time in a unique file called `state.json` that reflects the current state of all backup jobs. The file must include minimal information about each job, such as:

1. Name of the backup job
2. Hour and date of the last update
3. Whether the job is currently running or not

If the job is running:

- Total number of eligible files
- Size of files to transfer
- Progress
- Number of remaining files
- Size of remaining files
- Full address of the source file being backed up
- Full address of the destination file

In this implementation, `state.json` remains a per-job live status file and also stores the details of the most recent run for each configured job. The daily dated log file remains the full long-term history of all copied files across runs.

The locations of both files (daily log and real-time state) must be studied to work on our clients' servers. Avoid locations like `c:\temp\`.

All files (daily log and state) and configuration files must be in JSON format. For quick reading via Notepad, line breaks between JSON elements are necessary. Pagination would be a bonus.

## Centralized Docker logging

EasySave can now keep logs in three modes:

- `local`: daily logs are written only on the user's PC.
- `centralized`: daily logs are sent only to `EasyLog.Server`.
- `both`: daily logs are written locally and sent to `EasyLog.Server`.

`EasyLog.Server` is an ASP.NET HTTP service intended to run with Docker. It receives `POST /api/logs` requests and appends every entry to a single daily JSONL file named `yyyy-MM-dd.jsonl` in `/logs`, regardless of how many client machines send logs. Each entry includes `UserName`, `MachineName`, and `ClientId` so the source user and machine remain identifiable.

Run it from the repository root:

```powershell
docker compose up --build -d easylog-server
curl http://localhost:5080/health
```

On each EasySave client, configure the log storage mode, central server URL such as `http://<server-ip>:5080`, and the user name from either WPF settings or the console menu. If `EASYLOG_API_KEY` is set for the Docker service, configure the same API key on each client.
