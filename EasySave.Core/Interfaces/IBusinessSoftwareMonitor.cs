public interface IBusinessSoftwareMonitor
{
    bool TryGetRunningBlockedProcess(out string processName);
}
