namespace lazydotnet.Services;

public static class VsTestConsoleLocator
{
    private static string? _cachedPath;
    private static bool _hasSearched;

    public static string? GetVsTestConsolePath()
    {
        if (_hasSearched) return _cachedPath;

        var sdkPath = DotnetSdkResolver.GetLatestSdkPath();
        
        if (sdkPath != null)
        {
            var vstestPath = Path.Combine(sdkPath, "vstest.console.dll");
            
            if (File.Exists(vstestPath))
            {
                _cachedPath = vstestPath;
            }
        }

        _hasSearched = true;
        return _cachedPath;
    }
}