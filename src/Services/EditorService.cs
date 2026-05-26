using System.Diagnostics;
using CliWrap;
using lazydotnet.Core;
using lazydotnet.UI.Components;

namespace lazydotnet.Services;

public interface IEditorService
{
    string? RootPath { get; set; }
    Task OpenFileAsync(string filePath, int? lineNumber = null);
    (string Command, List<string> Args) GetEditorLaunchCommand(string filePath, int? lineNumber = null);
}

public class EditorService : IEditorService
{
    public string? RootPath { get; set; }

    private sealed record EditorCommand(string Command, List<string> PrefixArgs);

    private enum EditorType
    {
        VsCodeStyle,
        ZedStyle,
        VimStyle
    }

    private static readonly Dictionary<string, EditorType> TuiEditorTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["nvim"] = EditorType.VimStyle,
        ["vim"] = EditorType.VimStyle,
        ["vi"] = EditorType.VimStyle,
        ["nano"] = EditorType.VimStyle,
        ["pico"] = EditorType.VimStyle,
        ["emacs"] = EditorType.VimStyle,
        ["micro"] = EditorType.VimStyle,
        ["hx"] = EditorType.ZedStyle,
        ["helix"] = EditorType.ZedStyle
    };

    public static bool IsTuiEditor(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return false;
        var parsed = ParseEditorCommand(command);
        var name = Path.GetFileNameWithoutExtension(parsed.Command);
        return TuiEditorTypes.ContainsKey(name);
    }

    public async Task OpenFileAsync(string filePath, int? lineNumber = null)
    {
        var vscodeOutputFile = Environment.GetEnvironmentVariable("LAZYDOTNET_VSCODE_IPC_FILE");
        if (!string.IsNullOrEmpty(vscodeOutputFile))
        {
            var output = lineNumber.HasValue ? $"{filePath}\t{lineNumber}" : filePath;
            await File.WriteAllTextAsync(vscodeOutputFile, output);
            return;
        }

        var (command, args) = GetEditorLaunchCommand(filePath, lineNumber);

        if (IsTuiEditor(command))
        {
            await TuiSuspender.RunAsync(() => RunTuiEditorAsync(command, args));
            return;
        }

        try
        {
            await Cli.Wrap(command)
                .WithArguments(args)
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();
        }
        catch (Exception ex)
        {
            if (command != "open" && OperatingSystem.IsMacOS())
            {
                await Cli.Wrap("open").WithArguments(filePath).ExecuteAsync();
            }
            else
            {
                Notification.Show($"Failed to open editor: {ex.Message}", NotificationType.Error);
            }
        }
    }

    private static async Task RunTuiEditorAsync(string command, List<string> args)
    {
        try
        {
            var psi = new ProcessStartInfo(command)
            {
                UseShellExecute = false,
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };
            foreach (var a in args) psi.ArgumentList.Add(a);

            using var process = Process.Start(psi);
            if (process == null)
            {
                Notification.Show($"Failed to spawn editor: {command}", NotificationType.Error);
                return;
            }
            await process.WaitForExitAsync();
        }
        catch (Exception ex)
        {
            Notification.Show($"Editor crashed: {ex.Message}", NotificationType.Error);
        }
    }

    public (string Command, List<string> Args) GetEditorLaunchCommand(string filePath, int? lineNumber = null)
    {
        var (editor, type) = GetEditorInfo();
        var parsed = ParseEditorCommand(editor);
        var args = new List<string>(parsed.PrefixArgs);

        if (type is EditorType.VsCodeStyle or EditorType.ZedStyle)
        {
            args.Add(RootPath ?? Directory.GetCurrentDirectory());
        }

        switch (type)
        {
            case EditorType.VsCodeStyle:
                args.AddRange(GetVsCodeStyleArgs(filePath, lineNumber));
                break;

            case EditorType.ZedStyle:
                args.AddRange(GetZedStyleArgs(filePath, lineNumber));
                break;
            case EditorType.VimStyle:
                args.AddRange(GetVimStyleArgs(filePath, lineNumber));
                break;
            default:
                args.Add(lineNumber.HasValue ? $"{filePath}:{lineNumber}" : filePath);
                break;
        }

        return (parsed.Command, args);
    }

    private static IEnumerable<string> GetVimStyleArgs(string filePath, int? lineNumber)
    {
        if (lineNumber.HasValue)
        {
            yield return $"+{lineNumber}";
        }
        yield return filePath;
    }

    private static IEnumerable<string> GetVsCodeStyleArgs(string filePath, int? lineNumber)
    {
        if (lineNumber.HasValue)
        {
            yield return "--goto";
            yield return $"{filePath}:{lineNumber}";
        }
        else
        {
            yield return filePath;
        }
    }

    private static IEnumerable<string> GetZedStyleArgs(string filePath, int? lineNumber)
    {
        yield return lineNumber.HasValue ? $"{filePath}:{lineNumber}" : filePath;
    }

    private static (string Command, EditorType? Type) GetEditorInfo()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CURSOR_CLI")) ||
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CURSOR_AGENT")))
        {
            return ("cursor", EditorType.VsCodeStyle);
        }

        var antigravityAlias = Environment.GetEnvironmentVariable("ANTIGRAVITY_CLI_ALIAS");
        if (!string.IsNullOrEmpty(antigravityAlias))
        {
            return (antigravityAlias, EditorType.VsCodeStyle);
        }

        if (string.Equals(Environment.GetEnvironmentVariable("TERM_PROGRAM"), "zed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Environment.GetEnvironmentVariable("ZED_TERM"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return ("zed", EditorType.ZedStyle);
        }

        if (string.Equals(Environment.GetEnvironmentVariable("TERM_PROGRAM"), "vscode", StringComparison.OrdinalIgnoreCase))
        {
            return ("code", EditorType.VsCodeStyle);
        }

        var editor = Environment.GetEnvironmentVariable("EDITOR");
        if (string.IsNullOrEmpty(editor))
        {
            return ("code", EditorType.VsCodeStyle);
        }

        if (editor.Contains("cursor", StringComparison.OrdinalIgnoreCase) ||
            editor.Contains("code", StringComparison.OrdinalIgnoreCase))
        {
            return (editor, EditorType.VsCodeStyle);
        }
        if (editor.Contains("zed", StringComparison.OrdinalIgnoreCase))
        {
            return (editor, EditorType.ZedStyle);
        }

        var parsed = ParseEditorCommand(editor);
        var binaryName = Path.GetFileNameWithoutExtension(parsed.Command);
        if (TuiEditorTypes.TryGetValue(binaryName, out var tuiType))
        {
            return (editor, tuiType);
        }

        return (editor, null);
    }

    private static EditorCommand ParseEditorCommand(string command)
    {
        var tokens = SplitCommandLine(command);
        if (tokens.Count == 0)
        {
            return new EditorCommand(command, []);
        }
        return new EditorCommand(tokens[0], tokens.Skip(1).ToList());
    }

    private static List<string> SplitCommandLine(string command)
    {
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        char? quote = null;
        var escaping = false;

        foreach (var c in command)
        {
            if (escaping)
            {
                current.Append(c);
                escaping = false;
                continue;
            }

            if (c == '\\')
            {
                escaping = true;
                continue;
            }

            if (TryHandleQuotedChar(c, current, ref quote)) continue;
            if (TryHandleTokenBoundary(c, current, tokens)) continue;
            current.Append(c);
        }

        if (escaping)
        {
            current.Append('\\');
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        return tokens;
    }

    private static bool TryHandleQuotedChar(char c, System.Text.StringBuilder current, ref char? quote)
    {
        if (quote.HasValue)
        {
            if (c == quote.Value)
            {
                quote = null;
            }
            else
            {
                current.Append(c);
            }
            return true;
        }

        if (c is not ('\'' or '"')) return false;
        quote = c;
        return true;
    }

    private static bool TryHandleTokenBoundary(char c, System.Text.StringBuilder current, List<string> tokens)
    {
        if (!char.IsWhiteSpace(c)) return false;
        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
            current.Clear();
        }
        return true;
    }
}
