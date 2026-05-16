namespace CryptoSoft;

public static class Program
{
    private const string WindowsSingleInstanceMutexName = @"Global\ProSoft.CryptoSoft.SingleInstance";
    private const string SingleInstanceMutexName = "ProSoft.CryptoSoft.SingleInstance";
    private static readonly TimeSpan SingleInstanceTimeout = TimeSpan.FromMinutes(10);

    public static int Main(string[] args)
    {
        bool mutexAcquired = false;
        Mutex? mutex = null;

        try
        {
            if (args.Length != 2 || string.IsNullOrWhiteSpace(args[0]))
            {
                return ExitCodes.InvalidArguments;
            }

            if (string.IsNullOrWhiteSpace(args[1]))
            {
                return ExitCodes.InvalidKey;
            }

            mutex = new Mutex(false, GetSingleInstanceMutexName());

            try
            {
                mutexAcquired = mutex.WaitOne(SingleInstanceTimeout);
            }
            catch (AbandonedMutexException)
            {
                mutexAcquired = true;
            }

            if (!mutexAcquired)
            {
                return ExitCodes.BusyTimeout;
            }

            var fileManager = new FileManager(args[0], args[1]);
            return fileManager.TransformFile();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.Message);
            return ExitCodes.LaunchError;
        }
        finally
        {
            if (mutexAcquired && mutex is not null)
            {
                mutex.ReleaseMutex();
            }

            mutex?.Dispose();
        }
    }

    private static string GetSingleInstanceMutexName()
    {
        return OperatingSystem.IsWindows()
            ? WindowsSingleInstanceMutexName
            : SingleInstanceMutexName;
    }
}
