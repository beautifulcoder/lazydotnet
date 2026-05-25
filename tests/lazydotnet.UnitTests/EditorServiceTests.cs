using FluentAssertions;
using lazydotnet.Services;

namespace lazydotnet.UnitTests;

public class EditorServiceTests
{
    [Fact]
    public void GetEditorLaunchCommand_ShouldReturnCorrectArgs()
    {
        // Arrange
        var service = new EditorService();
        var filePath = "test.cs";
        var line = 10;

        // Act
        var (command, args) = service.GetEditorLaunchCommand(filePath, line);

        // Assert
        command.Should().NotBeNullOrEmpty();
        args.Should().Contain(a => a.Contains(filePath));
        if (args.Contains("--goto"))
        {
            args.Should().Contain($"{filePath}:{line}");
        }
    }

    [Theory]
    [InlineData("nvim")]
    [InlineData("NVIM")]
    [InlineData("vim")]
    [InlineData("vi")]
    [InlineData("helix")]
    [InlineData("hx")]
    [InlineData("nano")]
    [InlineData("micro")]
    [InlineData("pico")]
    [InlineData("emacs")]
    [InlineData("/usr/local/bin/nvim")]
    [InlineData("/opt/homebrew/bin/vim")]
    public void IsTuiEditor_TerminalEditors_ReturnsTrue(string command)
    {
        EditorService.IsTuiEditor(command).Should().BeTrue();
    }

    [Theory]
    [InlineData("code")]
    [InlineData("cursor")]
    [InlineData("zed")]
    [InlineData("subl")]
    [InlineData("rider")]
    [InlineData("/Applications/Visual Studio Code.app/Contents/MacOS/Electron")]
    public void IsTuiEditor_GuiEditors_ReturnsFalse(string command)
    {
        EditorService.IsTuiEditor(command).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void IsTuiEditor_EmptyOrWhitespace_ReturnsFalse(string command)
    {
        EditorService.IsTuiEditor(command).Should().BeFalse();
    }

    [Fact]
    public void IsTuiEditor_CommandWithArgs_DetectsBinaryName()
    {
        EditorService.IsTuiEditor("nvim -u NONE +42").Should().BeTrue();
        EditorService.IsTuiEditor("code --wait").Should().BeFalse();
    }

    [Fact]
    public void GetEditorLaunchCommand_VimStyleEditor_UsesPlusLineFile()
    {
        var original = Environment.GetEnvironmentVariable("EDITOR");
        try
        {
            Environment.SetEnvironmentVariable("EDITOR", "nvim");
            Environment.SetEnvironmentVariable("TERM_PROGRAM", null);
            Environment.SetEnvironmentVariable("ZED_TERM", null);
            Environment.SetEnvironmentVariable("CURSOR_CLI", null);
            Environment.SetEnvironmentVariable("CURSOR_AGENT", null);
            Environment.SetEnvironmentVariable("ANTIGRAVITY_CLI_ALIAS", null);

            var service = new EditorService();
            var (command, args) = service.GetEditorLaunchCommand("/path/file.cs", 42);

            command.Should().Be("nvim");
            args.Should().Equal("+42", "/path/file.cs");
        }
        finally
        {
            Environment.SetEnvironmentVariable("EDITOR", original);
        }
    }

    [Fact]
    public void GetEditorLaunchCommand_VimStyleEditor_NoLine_OmitsPlusFlag()
    {
        var original = Environment.GetEnvironmentVariable("EDITOR");
        try
        {
            Environment.SetEnvironmentVariable("EDITOR", "vim");
            Environment.SetEnvironmentVariable("TERM_PROGRAM", null);
            Environment.SetEnvironmentVariable("ZED_TERM", null);
            Environment.SetEnvironmentVariable("CURSOR_CLI", null);
            Environment.SetEnvironmentVariable("CURSOR_AGENT", null);
            Environment.SetEnvironmentVariable("ANTIGRAVITY_CLI_ALIAS", null);

            var service = new EditorService();
            var (command, args) = service.GetEditorLaunchCommand("/path/file.cs");

            command.Should().Be("vim");
            args.Should().Equal("/path/file.cs");
        }
        finally
        {
            Environment.SetEnvironmentVariable("EDITOR", original);
        }
    }

    [Fact]
    public void GetEditorLaunchCommand_VimStyle_DoesNotPrependRootPath()
    {
        var original = Environment.GetEnvironmentVariable("EDITOR");
        try
        {
            Environment.SetEnvironmentVariable("EDITOR", "nvim");
            Environment.SetEnvironmentVariable("TERM_PROGRAM", null);
            Environment.SetEnvironmentVariable("ZED_TERM", null);
            Environment.SetEnvironmentVariable("CURSOR_CLI", null);
            Environment.SetEnvironmentVariable("CURSOR_AGENT", null);
            Environment.SetEnvironmentVariable("ANTIGRAVITY_CLI_ALIAS", null);

            var service = new EditorService { RootPath = "/workspace/root" };
            var (_, args) = service.GetEditorLaunchCommand("/path/file.cs", 10);

            args.Should().NotContain("/workspace/root");
            args.Should().Equal("+10", "/path/file.cs");
        }
        finally
        {
            Environment.SetEnvironmentVariable("EDITOR", original);
        }
    }

    [Fact]
    public void GetEditorLaunchCommand_HelixEditor_UsesFileColonLine()
    {
        var original = Environment.GetEnvironmentVariable("EDITOR");
        try
        {
            Environment.SetEnvironmentVariable("EDITOR", "hx");
            Environment.SetEnvironmentVariable("TERM_PROGRAM", null);
            Environment.SetEnvironmentVariable("ZED_TERM", null);
            Environment.SetEnvironmentVariable("CURSOR_CLI", null);
            Environment.SetEnvironmentVariable("CURSOR_AGENT", null);
            Environment.SetEnvironmentVariable("ANTIGRAVITY_CLI_ALIAS", null);

            var service = new EditorService { RootPath = "/ws" };
            var (command, args) = service.GetEditorLaunchCommand("/path/file.cs", 5);

            command.Should().Be("hx");
            args.Should().Equal("/ws", "/path/file.cs:5");
        }
        finally
        {
            Environment.SetEnvironmentVariable("EDITOR", original);
        }
    }
}
