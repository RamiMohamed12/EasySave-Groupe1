using CryptoSoft;

namespace EasySave.Tests;

public class CryptoSoftTests
{
    [Fact]
    public void Main_ReturnsInvalidArguments_WhenFilePathIsMissing()
    {
        int exitCode = CryptoSoft.Program.Main([]);

        Assert.Equal(ExitCodes.InvalidArguments, exitCode);
    }

    [Fact]
    public void Main_ReturnsInvalidKey_WhenKeyIsEmpty()
    {
        int exitCode = CryptoSoft.Program.Main(["file.txt", ""]);

        Assert.Equal(ExitCodes.InvalidKey, exitCode);
    }

    [Fact]
    public void TransformFile_ReturnsFileNotFound_WhenFileDoesNotExist()
    {
        string missingFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.txt");
        var fileManager = new FileManager(missingFilePath, "secret");

        int exitCode = fileManager.TransformFile();

        Assert.Equal(ExitCodes.FileNotFound, exitCode);
    }

    [Fact]
    public void TransformFile_ReturnsInvalidKey_WhenKeyIsEmpty()
    {
        using var workspace = new TestWorkspace();
        string filePath = workspace.GetPath("secret.txt");
        File.WriteAllText(filePath, "content");
        var fileManager = new FileManager(filePath, "");

        int exitCode = fileManager.TransformFile();

        Assert.Equal(ExitCodes.InvalidKey, exitCode);
        Assert.Equal("content", File.ReadAllText(filePath));
    }

    [Fact]
    public void TransformFile_TransformsFileContent_WhenFileAndKeyAreValid()
    {
        using var workspace = new TestWorkspace();
        string filePath = workspace.GetPath("secret.txt");
        File.WriteAllText(filePath, "content");
        var fileManager = new FileManager(filePath, "key");

        int elapsedMilliseconds = fileManager.TransformFile();

        Assert.True(elapsedMilliseconds >= 0);
        Assert.NotEqual("content", File.ReadAllText(filePath));
    }
}
