BackupJob : one job with name,source,target,type
BackupType: full or differential
BackupService: The class that actually runs a backup 
LogEntry: one file transfer record with timestamp, backup name, source path, destination path, file size, and transfer time in ms
LoggerService in EasyLog: writes daily log lines
StateService: writes state.json 
ArgumentParser or similar: interprets 1-3 and 1;3 command line arguments to determine which jobs to run
