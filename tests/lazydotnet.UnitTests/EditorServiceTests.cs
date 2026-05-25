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
}
