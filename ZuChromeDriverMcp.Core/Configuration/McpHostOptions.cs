using Microsoft.Extensions.Configuration;
using Zu.Chrome;
using Zu.WebDriver.BasicTypes;

namespace ZuChromeDriverMcp.Core.Configuration;

public sealed class McpHostOptions
{
    public const string SectionName = "ZuChromeDriverMcp";

    public int Port { get; set; }

    public bool Headless { get; set; }

    public string UserDir { get; set; }

    public bool IsTempProfile { get; set; } = true;

    /// <summary>
    /// Id of the profile from <c>Profiles/profiles.json</c> applied to this session (if any).
    /// </summary>
    public string ActiveProfileId { get; set; }

    /// <summary>
    /// Display name of the active profile from the catalog (if any).
    /// </summary>
    public string ActiveProfileName { get; set; }

    public string CommandLineArguments { get; set; }

    /// <summary>
    /// When true, Chrome is not launched; the host attaches to an existing browser on <see cref="Port"/>.
    /// </summary>
    public bool AttachOnly { get; set; }

    public int? WindowWidth { get; set; }

    public int? WindowHeight { get; set; }

    public bool EnableFrameTrackerOnConnect { get; set; }

    public bool EnableDomTrackerOnConnect { get; set; }

    /// <summary>
    /// When true, <see cref="Browser.McpDevToolsCollector"/> subscribes to Network CDP events on connect and after page switch.
    /// </summary>
    public bool EnableNetworkCaptureOnConnect { get; set; } = true;

    /// <summary>
    /// When true, <see cref="Browser.McpDevToolsCollector"/> subscribes to Runtime console CDP events on connect and after page switch.
    /// </summary>
    public bool EnableConsoleCaptureOnConnect { get; set; } = true;

    public bool EnableBrowserLogCaptureOnConnect { get; set; }

    public bool EnableListNetworkRequestsTool { get; set; } = true;

    public bool EnableListConsoleMessagesTool { get; set; } = true;

    public bool EnableEvaluateTool { get; set; } = true;

    public bool EnableScreenshotTool { get; set; } = true;

    public bool EnableTakeSnapshotTool { get; set; } = true;

    /// <summary>
    /// Legacy: when true, enables both <see cref="EnableNetworkCaptureOnConnect"/> and <see cref="EnableConsoleCaptureOnConnect"/>.
    /// </summary>
    public bool EnableDevToolsCollectorOnConnect { get; set; }

    public McpCategoryOptions Categories { get; set; } = new();

    public McpTransportKind McpTransport { get; set; } = McpTransportKind.Http;

    public int McpHttpPort { get; set; } = 5100;

    public string McpHttpPath { get; set; } = "/mcp";

    /// <summary>
    /// When true, Chrome connects automatically at host startup (console Host default).
    /// WPF defaults to false — connect via UI or set explicitly.
    /// </summary>
    public bool ConnectChromeOnStartup { get; set; }

    /// <summary>
    /// Base location for MCP artifacts (screenshots, heap snapshots).
    /// Default: <see cref="McpArtifactsLocation.Executable"/> — <c>{exe_dir}/Temp/zu-chrome-driver-mcp</c>.
    /// </summary>
    public McpArtifactsLocation ArtifactsLocation { get; set; } = McpArtifactsLocation.Executable;

    public string GetArtifactsDirectory()
    {
        return ArtifactsLocation switch
        {
            McpArtifactsLocation.SystemTemp => Path.Combine(Path.GetTempPath(), "zu-chrome-driver-mcp"),
            _ => Path.Combine(AppContext.BaseDirectory, "Temp", "zu-chrome-driver-mcp"),
        };
    }

    public static McpHostOptions FromConfiguration(
        IConfiguration configuration,
        string[] args = null,
        bool defaultConnectOnStartup = false,
        McpHostSettingsSnapshot savedSettings = null)
    {
        var options = CreateFromSavedSettings(savedSettings, defaultConnectOnStartup);
        configuration.GetSection(SectionName).Bind(options);
        ApplyEnvironmentVariables(options);
        if (args != null && args.Length > 0)
            ApplyCommandLineArgs(options, args);

        if (string.IsNullOrWhiteSpace(options.UserDir))
            ChromeProfileResolver.TryApplyStartupProfile(options, options.ActiveProfileName);

        NormalizeLegacyOptions(options);
        return options;
    }

    static McpHostOptions CreateFromSavedSettings(McpHostSettingsSnapshot savedSettings, bool defaultConnectOnStartup)
    {
        if (savedSettings == null)
            return new McpHostOptions { ConnectChromeOnStartup = defaultConnectOnStartup };

        var options = new McpHostOptions();
        options.ApplySettingsSnapshot(savedSettings);
        return options;
    }

    public McpHostSettingsSnapshot ToSettingsSnapshot()
    {
        return new McpHostSettingsSnapshot
        {
            Port = Port,
            Headless = Headless,
            CommandLineArguments = CommandLineArguments,
            AttachOnly = AttachOnly,
            WindowWidth = WindowWidth,
            WindowHeight = WindowHeight,
            EnableFrameTrackerOnConnect = EnableFrameTrackerOnConnect,
            EnableDomTrackerOnConnect = EnableDomTrackerOnConnect,
            EnableBrowserLogCaptureOnConnect = EnableBrowserLogCaptureOnConnect,
            EnableNetworkCaptureOnConnect = EnableNetworkCaptureOnConnect,
            EnableConsoleCaptureOnConnect = EnableConsoleCaptureOnConnect,
            EnableListNetworkRequestsTool = EnableListNetworkRequestsTool,
            EnableListConsoleMessagesTool = EnableListConsoleMessagesTool,
            EnableEvaluateTool = EnableEvaluateTool,
            EnableScreenshotTool = EnableScreenshotTool,
            EnableTakeSnapshotTool = EnableTakeSnapshotTool,
            CategoryInput = Categories.Input,
            CategoryNavigation = Categories.Navigation,
            CategoryMemory = Categories.Memory,
            CategoryEmulation = Categories.Emulation,
            McpHttpPort = McpHttpPort,
            McpHttpPath = McpHttpPath,
            ConnectChromeOnStartup = ConnectChromeOnStartup,
            ArtifactsLocation = ArtifactsLocation,
        };
    }

    public void ApplySettingsSnapshot(McpHostSettingsSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        Port = snapshot.Port;
        Headless = snapshot.Headless;
        CommandLineArguments = snapshot.CommandLineArguments;
        AttachOnly = snapshot.AttachOnly;
        WindowWidth = snapshot.WindowWidth;
        WindowHeight = snapshot.WindowHeight;
        EnableFrameTrackerOnConnect = snapshot.EnableFrameTrackerOnConnect;
        EnableDomTrackerOnConnect = snapshot.EnableDomTrackerOnConnect;
        EnableBrowserLogCaptureOnConnect = snapshot.EnableBrowserLogCaptureOnConnect;
        EnableNetworkCaptureOnConnect = snapshot.EnableNetworkCaptureOnConnect;
        EnableConsoleCaptureOnConnect = snapshot.EnableConsoleCaptureOnConnect;
        EnableListNetworkRequestsTool = snapshot.EnableListNetworkRequestsTool;
        EnableListConsoleMessagesTool = snapshot.EnableListConsoleMessagesTool;
        EnableEvaluateTool = snapshot.EnableEvaluateTool;
        EnableScreenshotTool = snapshot.EnableScreenshotTool;
        EnableTakeSnapshotTool = snapshot.EnableTakeSnapshotTool;
        Categories.Input = snapshot.CategoryInput;
        Categories.Navigation = snapshot.CategoryNavigation;
        Categories.Memory = snapshot.CategoryMemory;
        Categories.Emulation = snapshot.CategoryEmulation;
        McpHttpPort = snapshot.McpHttpPort;
        McpHttpPath = snapshot.McpHttpPath;
        ConnectChromeOnStartup = snapshot.ConnectChromeOnStartup;
        ArtifactsLocation = snapshot.ArtifactsLocation;

        SyncLegacyCategoriesFromToolFlags();
    }

    /// <summary>
    /// Keeps in-memory <see cref="McpCategoryOptions.Network"/> / <see cref="McpCategoryOptions.Debugging"/>
    /// aligned with per-tool flags (Host env/CLI compatibility only; not persisted).
    /// </summary>
    public void SyncLegacyCategoriesFromToolFlags()
    {
        Categories.Network = EnableListNetworkRequestsTool;
        Categories.Debugging = EnableEvaluateTool
            && EnableScreenshotTool
            && EnableTakeSnapshotTool
            && EnableListConsoleMessagesTool;
    }

    public ChromeDriverConfig ToChromeDriverConfig()
    {
        var config = new ChromeDriverConfig
        {
            Port = Port,
            Headless = Headless,
            CommandLineArguments = CommandLineArguments,
            DoNotOpenChromeProfile = AttachOnly,
            IsTempProfile = IsTempProfile,
            EnableFrameTrackerOnConnect = EnableFrameTrackerOnConnect,
            EnableDomTrackerOnConnect = EnableDomTrackerOnConnect,
            EnableBrowserLogCaptureOnConnect = EnableBrowserLogCaptureOnConnect,
        };

        if (!string.IsNullOrWhiteSpace(UserDir))
            config.SetUserDir(UserDir);

        if (WindowWidth.HasValue && WindowHeight.HasValue)
            config.SetWindowSize(WindowWidth.Value, WindowHeight.Value);

        config.LoggingPreferences[LogType.Browser] = LogLevel.All;

        return config;
    }

    static void ApplyCommandLineArgs(McpHostOptions options, string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
                continue;

            var body = arg.Substring(2);
            var eq = body.IndexOf('=');
            string key;
            string value;
            if (eq >= 0)
            {
                key = body.Substring(0, eq);
                value = body.Substring(eq + 1);
            }
            else
            {
                key = body;
                value = "true";
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    value = args[++i];
                }
            }

            if (ApplyHostFlag(options, key, value))
                continue;

            ApplyCategoryFlag(options, key, value);
        }
    }

    static bool ApplyHostFlag(McpHostOptions options, string key, string value)
    {
        switch (key.ToLowerInvariant())
        {
            case "stdio":
                if (TryParseBool(value, out var stdio) && stdio)
                    options.McpTransport = McpTransportKind.Stdio;
                return true;
            case "transport":
                if (string.Equals(value, "stdio", StringComparison.OrdinalIgnoreCase))
                    options.McpTransport = McpTransportKind.Stdio;
                else if (string.Equals(value, "http", StringComparison.OrdinalIgnoreCase))
                    options.McpTransport = McpTransportKind.Http;
                return true;
            case "mcp-http-port":
                if (int.TryParse(value, out var httpPort))
                    options.McpHttpPort = httpPort;
                return true;
            case "mcp-http-path":
                if (!string.IsNullOrWhiteSpace(value))
                    options.McpHttpPath = value.StartsWith("/", StringComparison.Ordinal) ? value : "/" + value;
                return true;
            case "connect-on-startup":
                if (TryParseBool(value, out var connectOnStartup))
                    options.ConnectChromeOnStartup = connectOnStartup;
                return true;
            case "artifacts-location":
                if (TryParseArtifactsLocation(value, out var artifactsLocation))
                    options.ArtifactsLocation = artifactsLocation;
                return true;
            case "enable-frame-tracker":
                if (TryParseBool(value, out var frameTracker))
                    options.EnableFrameTrackerOnConnect = frameTracker;
                return true;
            case "enable-dom-tracker":
                if (TryParseBool(value, out var domTracker))
                    options.EnableDomTrackerOnConnect = domTracker;
                return true;
            case "enable-devtools-collector":
                if (TryParseBool(value, out var devToolsCollector))
                    options.EnableDevToolsCollectorOnConnect = devToolsCollector;
                return true;
            case "enable-network-capture":
                if (TryParseBool(value, out var networkCapture))
                    options.EnableNetworkCaptureOnConnect = networkCapture;
                return true;
            case "enable-console-capture":
                if (TryParseBool(value, out var consoleCapture))
                    options.EnableConsoleCaptureOnConnect = consoleCapture;
                return true;
            case "enable-list-network-requests-tool":
                if (TryParseBool(value, out var listNetworkTool))
                    options.EnableListNetworkRequestsTool = listNetworkTool;
                return true;
            case "enable-list-console-messages-tool":
                if (TryParseBool(value, out var listConsoleTool))
                    options.EnableListConsoleMessagesTool = listConsoleTool;
                return true;
            case "enable-evaluate-tool":
                if (TryParseBool(value, out var evaluateTool))
                    options.EnableEvaluateTool = evaluateTool;
                return true;
            case "enable-screenshot-tool":
                if (TryParseBool(value, out var screenshotTool))
                    options.EnableScreenshotTool = screenshotTool;
                return true;
            case "enable-take-snapshot-tool":
                if (TryParseBool(value, out var snapshotTool))
                    options.EnableTakeSnapshotTool = snapshotTool;
                return true;
            case "profile":
                if (!string.IsNullOrWhiteSpace(value))
                    options.ActiveProfileName = value;
                return true;
            default:
                return false;
        }
    }

    static void ApplyCategoryFlag(McpHostOptions options, string key, string value)
    {
        if (!TryParseBool(value, out var enabled))
            return;

        var categories = options.Categories;
        switch (key.ToLowerInvariant())
        {
            case "category-input":
                categories.Input = enabled;
                break;
            case "category-navigation":
                categories.Navigation = enabled;
                break;
            case "category-emulation":
                categories.Emulation = enabled;
                break;
            case "category-network":
                categories.Network = enabled;
                options.EnableListNetworkRequestsTool = enabled;
                break;
            case "category-debugging":
                categories.Debugging = enabled;
                options.EnableEvaluateTool = enabled;
                options.EnableScreenshotTool = enabled;
                options.EnableTakeSnapshotTool = enabled;
                options.EnableListConsoleMessagesTool = enabled;
                break;
            case "category-memory":
                categories.Memory = enabled;
                break;
        }
    }

    static void ApplyEnvironmentVariables(McpHostOptions options)
    {
        if (TryReadIntEnvironment("ZU_CHROME_DRIVER_MCP_PORT", out var port))
            options.Port = port;

        if (TryReadBoolEnvironment("ZU_CHROME_DRIVER_MCP_HEADLESS", out var headless))
            options.Headless = headless;

        var userDir = Environment.GetEnvironmentVariable("ZU_CHROME_DRIVER_MCP_USER_DIR");
        if (!string.IsNullOrWhiteSpace(userDir))
        {
            options.UserDir = userDir;
            options.IsTempProfile = false;
        }

        if (TryReadBoolEnvironment("ZU_CHROME_DRIVER_MCP_TEMP_PROFILE", out var tempProfile))
            options.IsTempProfile = tempProfile;

        var profileName = Environment.GetEnvironmentVariable("ZU_CHROME_DRIVER_MCP_PROFILE");
        if (!string.IsNullOrWhiteSpace(profileName))
            options.ActiveProfileName = profileName;

        var chromeArgs = Environment.GetEnvironmentVariable("ZU_CHROME_DRIVER_MCP_CHROME_ARGS");
        if (!string.IsNullOrWhiteSpace(chromeArgs))
            options.CommandLineArguments = chromeArgs;

        if (TryReadBoolEnvironment("ZU_CHROME_DRIVER_MCP_ATTACH_ONLY", out var attachOnly))
            options.AttachOnly = attachOnly;

        if (TryReadIntEnvironment("ZU_CHROME_DRIVER_MCP_WINDOW_WIDTH", out var width))
            options.WindowWidth = width;

        if (TryReadIntEnvironment("ZU_CHROME_DRIVER_MCP_WINDOW_HEIGHT", out var height))
            options.WindowHeight = height;

        if (TryReadBoolEnvironment("ZU_CHROME_DRIVER_MCP_ENABLE_FRAME_TRACKER", out var frameTracker))
            options.EnableFrameTrackerOnConnect = frameTracker;

        if (TryReadBoolEnvironment("ZU_CHROME_DRIVER_MCP_ENABLE_DOM_TRACKER", out var domTracker))
            options.EnableDomTrackerOnConnect = domTracker;

        if (TryReadBoolEnvironment("ZU_CHROME_DRIVER_MCP_ENABLE_DEVTOOLS_COLLECTOR", out var devToolsCollector))
            options.EnableDevToolsCollectorOnConnect = devToolsCollector;

        if (TryReadBoolEnvironment("ZU_CHROME_DRIVER_MCP_ENABLE_NETWORK_CAPTURE", out var networkCapture))
            options.EnableNetworkCaptureOnConnect = networkCapture;

        if (TryReadBoolEnvironment("ZU_CHROME_DRIVER_MCP_ENABLE_CONSOLE_CAPTURE", out var consoleCapture))
            options.EnableConsoleCaptureOnConnect = consoleCapture;

        if (TryReadBoolEnvironment("ZU_CHROME_DRIVER_MCP_ENABLE_LIST_NETWORK_REQUESTS_TOOL", out var listNetworkTool))
            options.EnableListNetworkRequestsTool = listNetworkTool;

        if (TryReadBoolEnvironment("ZU_CHROME_DRIVER_MCP_ENABLE_LIST_CONSOLE_MESSAGES_TOOL", out var listConsoleTool))
            options.EnableListConsoleMessagesTool = listConsoleTool;

        if (TryReadBoolEnvironment("ZU_CHROME_DRIVER_MCP_ENABLE_EVALUATE_TOOL", out var evaluateTool))
            options.EnableEvaluateTool = evaluateTool;

        if (TryReadBoolEnvironment("ZU_CHROME_DRIVER_MCP_ENABLE_SCREENSHOT_TOOL", out var screenshotTool))
            options.EnableScreenshotTool = screenshotTool;

        if (TryReadBoolEnvironment("ZU_CHROME_DRIVER_MCP_ENABLE_TAKE_SNAPSHOT_TOOL", out var snapshotTool))
            options.EnableTakeSnapshotTool = snapshotTool;

        if (TryReadBoolEnvironment("ZU_CHROME_DRIVER_MCP_CATEGORY_INPUT", out var categoryInput))
            options.Categories.Input = categoryInput;
        if (TryReadBoolEnvironment("ZU_CHROME_DRIVER_MCP_CATEGORY_NAVIGATION", out var categoryNavigation))
            options.Categories.Navigation = categoryNavigation;
        if (TryReadBoolEnvironment("ZU_CHROME_DRIVER_MCP_CATEGORY_EMULATION", out var categoryEmulation))
            options.Categories.Emulation = categoryEmulation;
        if (TryReadBoolEnvironment("ZU_CHROME_DRIVER_MCP_CATEGORY_NETWORK", out var categoryNetwork))
        {
            options.Categories.Network = categoryNetwork;
            options.EnableListNetworkRequestsTool = categoryNetwork;
        }

        if (TryReadBoolEnvironment("ZU_CHROME_DRIVER_MCP_CATEGORY_DEBUGGING", out var categoryDebugging))
        {
            options.Categories.Debugging = categoryDebugging;
            options.EnableEvaluateTool = categoryDebugging;
            options.EnableScreenshotTool = categoryDebugging;
            options.EnableTakeSnapshotTool = categoryDebugging;
            options.EnableListConsoleMessagesTool = categoryDebugging;
        }
        if (TryReadBoolEnvironment("ZU_CHROME_DRIVER_MCP_CATEGORY_MEMORY", out var categoryMemory))
            options.Categories.Memory = categoryMemory;

        var transport = Environment.GetEnvironmentVariable("ZU_CHROME_DRIVER_MCP_TRANSPORT");
        if (string.Equals(transport, "stdio", StringComparison.OrdinalIgnoreCase))
            options.McpTransport = McpTransportKind.Stdio;
        else if (string.Equals(transport, "http", StringComparison.OrdinalIgnoreCase))
            options.McpTransport = McpTransportKind.Http;

        if (TryReadIntEnvironment("ZU_CHROME_DRIVER_MCP_MCP_HTTP_PORT", out var mcpHttpPort))
            options.McpHttpPort = mcpHttpPort;

        var mcpHttpPath = Environment.GetEnvironmentVariable("ZU_CHROME_DRIVER_MCP_MCP_HTTP_PATH");
        if (!string.IsNullOrWhiteSpace(mcpHttpPath))
            options.McpHttpPath = mcpHttpPath.StartsWith("/", StringComparison.Ordinal) ? mcpHttpPath : "/" + mcpHttpPath;

        if (TryReadBoolEnvironment("ZU_CHROME_DRIVER_MCP_CONNECT_ON_STARTUP", out var connectOnStartup))
            options.ConnectChromeOnStartup = connectOnStartup;

        var artifactsLocation = Environment.GetEnvironmentVariable("ZU_CHROME_DRIVER_MCP_ARTIFACTS_LOCATION");
        if (TryParseArtifactsLocation(artifactsLocation, out var parsedArtifactsLocation))
            options.ArtifactsLocation = parsedArtifactsLocation;
    }

    /// <summary>
    /// Applies tool visibility and browser-behavior flags without Chrome connect settings (port, profile, window).
    /// Safe while Chrome is connected; takes effect on the next <c>list_tools</c> without restart.
    /// </summary>
    public void ApplyLiveSettingsFrom(McpHostOptions source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        EnableFrameTrackerOnConnect = source.EnableFrameTrackerOnConnect;
        EnableDomTrackerOnConnect = source.EnableDomTrackerOnConnect;
        EnableBrowserLogCaptureOnConnect = source.EnableBrowserLogCaptureOnConnect;
        EnableNetworkCaptureOnConnect = source.EnableNetworkCaptureOnConnect;
        EnableConsoleCaptureOnConnect = source.EnableConsoleCaptureOnConnect;
        EnableListNetworkRequestsTool = source.EnableListNetworkRequestsTool;
        EnableListConsoleMessagesTool = source.EnableListConsoleMessagesTool;
        EnableEvaluateTool = source.EnableEvaluateTool;
        EnableScreenshotTool = source.EnableScreenshotTool;
        EnableTakeSnapshotTool = source.EnableTakeSnapshotTool;
        Categories.Input = source.Categories.Input;
        Categories.Navigation = source.Categories.Navigation;
        Categories.Memory = source.Categories.Memory;
        SyncLegacyCategoriesFromToolFlags();
    }

    public void CopyFrom(McpHostOptions source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        Port = source.Port;
        Headless = source.Headless;
        UserDir = source.UserDir;
        IsTempProfile = source.IsTempProfile;
        ActiveProfileId = source.ActiveProfileId;
        ActiveProfileName = source.ActiveProfileName;
        CommandLineArguments = source.CommandLineArguments;
        AttachOnly = source.AttachOnly;
        WindowWidth = source.WindowWidth;
        WindowHeight = source.WindowHeight;
        EnableFrameTrackerOnConnect = source.EnableFrameTrackerOnConnect;
        EnableDomTrackerOnConnect = source.EnableDomTrackerOnConnect;
        EnableNetworkCaptureOnConnect = source.EnableNetworkCaptureOnConnect;
        EnableConsoleCaptureOnConnect = source.EnableConsoleCaptureOnConnect;
        EnableDevToolsCollectorOnConnect = source.EnableDevToolsCollectorOnConnect;
        EnableBrowserLogCaptureOnConnect = source.EnableBrowserLogCaptureOnConnect;
        EnableListNetworkRequestsTool = source.EnableListNetworkRequestsTool;
        EnableListConsoleMessagesTool = source.EnableListConsoleMessagesTool;
        EnableEvaluateTool = source.EnableEvaluateTool;
        EnableScreenshotTool = source.EnableScreenshotTool;
        EnableTakeSnapshotTool = source.EnableTakeSnapshotTool;
        McpTransport = source.McpTransport;
        McpHttpPort = source.McpHttpPort;
        McpHttpPath = source.McpHttpPath;
        ConnectChromeOnStartup = source.ConnectChromeOnStartup;
        ArtifactsLocation = source.ArtifactsLocation;

        Categories.Input = source.Categories.Input;
        Categories.Navigation = source.Categories.Navigation;
        Categories.Emulation = source.Categories.Emulation;
        Categories.Memory = source.Categories.Memory;
        SyncLegacyCategoriesFromToolFlags();
    }

    public string GetHttpEndpointUrl()
    {
        var path = string.IsNullOrWhiteSpace(McpHttpPath) ? "/mcp" : McpHttpPath;
        if (!path.StartsWith("/", StringComparison.Ordinal))
            path = "/" + path;
        return $"http://127.0.0.1:{McpHttpPort}{path}";
    }

    static bool TryReadIntEnvironment(string name, out int value)
    {
        value = default;
        var text = Environment.GetEnvironmentVariable(name);
        return !string.IsNullOrWhiteSpace(text) && int.TryParse(text, out value);
    }

    static bool TryReadBoolEnvironment(string name, out bool value)
    {
        value = default;
        var text = Environment.GetEnvironmentVariable(name);
        return TryParseBool(text, out value);
    }

    static bool TryParseBool(string text, out bool value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (bool.TryParse(text, out value))
            return true;

        if (string.Equals(text, "1", StringComparison.OrdinalIgnoreCase))
        {
            value = true;
            return true;
        }

        if (string.Equals(text, "0", StringComparison.OrdinalIgnoreCase))
        {
            value = false;
            return true;
        }

        return false;
    }

    static void NormalizeLegacyOptions(McpHostOptions options)
    {
        if (options.EnableDevToolsCollectorOnConnect)
        {
            options.EnableNetworkCaptureOnConnect = true;
            options.EnableConsoleCaptureOnConnect = true;
        }

        options.SyncLegacyCategoriesFromToolFlags();
    }

    static bool TryParseArtifactsLocation(string text, out McpArtifactsLocation value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        switch (text.Trim().ToLowerInvariant())
        {
            case "executable":
            case "exe":
                value = McpArtifactsLocation.Executable;
                return true;
            case "system-temp":
            case "systemtemp":
            case "temp":
                value = McpArtifactsLocation.SystemTemp;
                return true;
            default:
                return false;
        }
    }
}
