using System.Runtime.InteropServices;

public sealed class DirectorySearchResult
{
    public DirectorySearchResult(IReadOnlyList<string> directories, bool wasLimitReached)
    {
        Directories = directories;
        WasLimitReached = wasLimitReached;
    }

    public IReadOnlyList<string> Directories { get; }

    public bool WasLimitReached { get; }
}

public static class FastDirectorySearch
{
    public const int DefaultResultLimit = 100;

    private const int FileAttributeDirectory = 0x10;
    private const int FileAttributeReparsePoint = 0x400;
    private const int FindFirstExLargeFetch = 0x2;
    private static readonly IntPtr InvalidHandleValue = new(-1);

    public static DirectorySearchResult Search(string rootDirectory, string query, int resultLimit = DefaultResultLimit)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException();
        }

        string trimmedQuery = query.Trim();
        var matches = new List<string>(Math.Min(resultLimit, DefaultResultLimit));
        SearchDirectory(rootDirectory, trimmedQuery, resultLimit, matches);

        return new DirectorySearchResult(matches, matches.Count >= resultLimit);
    }

    public static bool IsNameMatch(string directoryPath, string query)
    {
        string directoryName = directoryPath
            .TrimEnd('\\', '/')
            .Split('\\', '/')
            .LastOrDefault() ?? string.Empty;

        return directoryName.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private static void SearchDirectory(string directory, string query, int resultLimit, List<string> matches)
    {
        if (matches.Count >= resultLimit)
        {
            return;
        }

        IntPtr searchHandle = FindFirstFileExW(
            Path.Combine(directory, "*"),
            FINDEX_INFO_LEVELS.FindExInfoBasic,
            out WIN32_FIND_DATA findData,
            FINDEX_SEARCH_OPS.FindExSearchNameMatch,
            IntPtr.Zero,
            FindFirstExLargeFetch);

        if (searchHandle == InvalidHandleValue)
        {
            return;
        }

        try
        {
            do
            {
                if (matches.Count >= resultLimit)
                {
                    return;
                }

                string fileName = findData.cFileName;
                if (!IsDirectory(findData) || IsReparsePoint(findData) || fileName is "." or "..")
                {
                    continue;
                }

                string childDirectory = Path.Combine(directory, fileName);
                if (IsNameMatch(childDirectory, query))
                {
                    matches.Add(childDirectory);
                }

                SearchDirectory(childDirectory, query, resultLimit, matches);
            }
            while (FindNextFileW(searchHandle, out findData));

        }
        finally
        {
            FindClose(searchHandle);
        }
    }

    private static bool IsDirectory(WIN32_FIND_DATA findData)
    {
        return (findData.dwFileAttributes & FileAttributeDirectory) == FileAttributeDirectory;
    }

    private static bool IsReparsePoint(WIN32_FIND_DATA findData)
    {
        return (findData.dwFileAttributes & FileAttributeReparsePoint) == FileAttributeReparsePoint;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindFirstFileExW(
        string lpFileName,
        FINDEX_INFO_LEVELS fInfoLevelId,
        out WIN32_FIND_DATA lpFindFileData,
        FINDEX_SEARCH_OPS fSearchOp,
        IntPtr lpSearchFilter,
        int dwAdditionalFlags);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool FindNextFileW(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FindClose(IntPtr hFindFile);

    private enum FINDEX_INFO_LEVELS
    {
        FindExInfoStandard,
        FindExInfoBasic
    }

    private enum FINDEX_SEARCH_OPS
    {
        FindExSearchNameMatch
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WIN32_FIND_DATA
    {
        public int dwFileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
        public int nFileSizeHigh;
        public int nFileSizeLow;
        public int dwReserved0;
        public int dwReserved1;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string cFileName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string cAlternateFileName;
    }
}
