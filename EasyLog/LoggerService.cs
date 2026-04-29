using System.Text.Json;
using System.Xml.Linq;

public class LoggerService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public void WriteLog(LogEntry entry)
    {
        string logFilePath = RuntimeStoragePaths.GetDailyLogFilePath(entry.Timestamp);

        if (RuntimeStoragePaths.GetLogFileFormat() == RuntimeStoragePaths.XmlLogFileFormat)
        {
            WriteXmlLog(logFilePath, entry);
            return;
        }

        string jsonLine = JsonSerializer.Serialize(entry, SerializerOptions);

        File.AppendAllText(logFilePath, jsonLine + Environment.NewLine);
    }

    private static void WriteXmlLog(string logFilePath, LogEntry entry)
    {
        XDocument document;
        XElement root;

        if (File.Exists(logFilePath))
        {
            document = XDocument.Load(logFilePath);
            root = document.Root ?? new XElement("LogEntries");

            if (document.Root is null)
            {
                document.Add(root);
            }
        }
        else
        {
            root = new XElement("LogEntries");
            document = new XDocument(root);
        }

        root.Add(
            new XElement("LogEntry",
                new XElement(nameof(LogEntry.Timestamp), entry.Timestamp.ToString("O")),
                new XElement(nameof(LogEntry.BackupName), entry.BackupName),
                new XElement(nameof(LogEntry.SourcePath), entry.SourcePath),
                new XElement(nameof(LogEntry.DestinationPath), entry.DestinationPath),
                new XElement(nameof(LogEntry.ActionType), entry.ActionType),
                new XElement(nameof(LogEntry.ErrorMessage), entry.ErrorMessage),
                new XElement(nameof(LogEntry.FileSizeBytes), entry.FileSizeBytes),
                new XElement(nameof(LogEntry.TransferTimeMilliseconds), entry.TransferTimeMilliseconds)));

        document.Save(logFilePath);
    }
}
