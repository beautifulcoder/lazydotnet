using Microsoft.Build.Locator;
using lazydotnet.Commands;
using lazydotnet.Services;
using Spectre.Console;
using Spectre.Console.Cli;

Console.OutputEncoding = System.Text.Encoding.UTF8;

if (!MSBuildLocator.IsRegistered)
{
    try 
    {
        MSBuildLocator.RegisterDefaults();
    }
    catch
    {
        var manualPath = DotnetSdkResolver.GetLatestSdkPath();
        
        if (!string.IsNullOrEmpty(manualPath))
        {
            MSBuildLocator.RegisterMSBuildPath(manualPath);
        }
    }

    if (!MSBuildLocator.IsRegistered)
    {
        AnsiConsole.MarkupLine("[red]Fatal Error:[/] Could not locate a .NET SDK.");
        AnsiConsole.MarkupLine("Please ensure [yellow]dotnet[/] is installed and available in your PATH.");
        return 1;
    }
}

var app = new CommandApp<DefaultCommand>();
return await app.RunAsync(args);