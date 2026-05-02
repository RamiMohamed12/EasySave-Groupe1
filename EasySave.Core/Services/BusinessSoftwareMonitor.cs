using System.Diagnostics;

public class BusinessSoftwareMonitor : IBusinessSoftwareMonitor
{
    public bool TryGetRunningBlockedProcess(out string processName)
    {
        foreach (string blockedProcessName in RuntimeStoragePaths.GetBlockedProcessNames())
        {
            if (Process.GetProcessesByName(blockedProcessName).Length > 0)
            {
                processName = blockedProcessName;
                return true;
            }
        }

        processName = string.Empty;
        return false;
    }
}
