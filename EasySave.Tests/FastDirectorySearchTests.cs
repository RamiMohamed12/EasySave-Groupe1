public class FastDirectorySearchTests
{
    [Fact]
    public void IsNameMatch_MatchesFinalDirectoryNameCaseInsensitively()
    {
        Assert.True(FastDirectorySearch.IsNameMatch(@"C:\Users\Public\Reports", "ports"));
        Assert.True(FastDirectorySearch.IsNameMatch(@"C:\Users\Public\Reports", "REPORTS"));
    }

    [Fact]
    public void IsNameMatch_DoesNotMatchParentDirectoryNames()
    {
        Assert.False(FastDirectorySearch.IsNameMatch(@"C:\Projects\EasySave\Data", "EasySave"));
    }
}
