namespace EasySave.Tests;

public class ArgumentParserTests
{
    private readonly ArgumentParser _parser;

    public ArgumentParserTests()
    {
        _parser = new ArgumentParser(ApplicationTextService.Create(ApplicationTextService.EnglishLanguageCode));
    }

    [Fact]
    public void Parse_ReturnsShowJobs_WhenNoArgumentIsProvided()
    {
        CliCommand command = _parser.Parse(Array.Empty<string>());

        Assert.Equal(CliCommandType.ShowJobs, command.Type);
        Assert.Empty(command.SelectedJobNumbers);
    }

    [Fact]
    public void Parse_ReturnsShowHelp_WhenHelpArgumentIsProvided()
    {
        CliCommand command = _parser.Parse(["--help"]);

        Assert.Equal(CliCommandType.ShowHelp, command.Type);
    }

    [Fact]
    public void Parse_ReturnsRunSelection_ForSemicolonSelection()
    {
        CliCommand command = _parser.Parse(["3;1;3"]);

        Assert.Equal(CliCommandType.RunSelection, command.Type);
        Assert.Equal([1, 3], command.SelectedJobNumbers);
    }

    [Fact]
    public void Parse_ReturnsConfigureJobPath_ForConfigureCommand()
    {
        CliCommand command = _parser.Parse(["--configure", "2", "target", @"E:\Backup"]);

        Assert.Equal(CliCommandType.ConfigureJobPath, command.Type);
        Assert.Equal(2, command.JobNumber);
        Assert.Equal(JobPathField.Target, command.PathField);
        Assert.Equal(@"E:\Backup", command.PathValue);
    }

    [Fact]
    public void Parse_ReturnsConfigureStorageDirectory_ForStorageCommand()
    {
        CliCommand command = _parser.Parse(["--storage-dir", @"F:\EasySaveData"]);

        Assert.Equal(CliCommandType.ConfigureStorageDirectory, command.Type);
        Assert.Equal(@"F:\EasySaveData", command.PathValue);
    }

    [Fact]
    public void Parse_ReturnsConfigureLanguage_ForLanguageCommand()
    {
        CliCommand command = _parser.Parse(["--lang", "fr"]);

        Assert.Equal(CliCommandType.ConfigureLanguage, command.Type);
        Assert.Equal("fr", command.LanguageCode);
    }

    [Fact]
    public void Parse_Throws_ForInvalidConfigureField()
    {
        Assert.Throws<ArgumentException>(() => _parser.Parse(["--configure", "1", "name", @"C:\Data"]));
    }

    [Fact]
    public void Parse_Throws_ForInvalidLanguageCode()
    {
        Assert.Throws<ArgumentException>(() => _parser.Parse(["--lang", "de"]));
    }
}
