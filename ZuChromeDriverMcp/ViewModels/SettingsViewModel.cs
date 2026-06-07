using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZuChromeDriverMcp.Core.Browser;
using ZuChromeDriverMcp.Core.Configuration;

namespace ZuChromeDriverMcp.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    readonly McpHostOptions _options;
    readonly McpOperatorService _operatorService;
    bool _suppressDependencyDialogs;

    public SettingsViewModel(McpHostOptions options, McpOperatorService operatorService)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _operatorService = operatorService ?? throw new ArgumentNullException(nameof(operatorService));
        LoadFromOptions();
    }

    public event EventHandler<string> StatusChanged;

    [ObservableProperty]
    private int _port;

    [ObservableProperty]
    private bool _headless;

    [ObservableProperty]
    private string _commandLineArguments = "";

    [ObservableProperty]
    private bool _attachOnly;

    [ObservableProperty]
    private int? _windowWidth;

    [ObservableProperty]
    private int? _windowHeight;

    [ObservableProperty]
    private bool _enableFrameTrackerOnConnect;

    [ObservableProperty]
    private bool _enableDomTrackerOnConnect;

    [ObservableProperty]
    private bool _enableBrowserLogCaptureOnConnect;

    [ObservableProperty]
    private bool _enableNetworkCaptureOnConnect;

    [ObservableProperty]
    private bool _enableConsoleCaptureOnConnect;

    [ObservableProperty]
    private bool _enableListNetworkRequestsTool;

    [ObservableProperty]
    private bool _enableListConsoleMessagesTool;

    [ObservableProperty]
    private bool _enableEvaluateTool;

    [ObservableProperty]
    private bool _enableScreenshotTool;

    [ObservableProperty]
    private bool _enableTakeSnapshotTool;

    [ObservableProperty]
    private bool _categoryInput = true;

    [ObservableProperty]
    private bool _categoryNavigation = true;

    [ObservableProperty]
    private bool _categoryMemory = true;

    [ObservableProperty]
    private int _mcpHttpPort = 5100;

    [ObservableProperty]
    private string _mcpHttpPath = "/mcp";

    [ObservableProperty]
    private bool _connectChromeOnStartup;

    [RelayCommand]
    void ApplySettings()
    {
        var draft = CreateDraftFromViewModel();
        if (_operatorService.IsConnected && HasConnectionSensitiveChanges(draft))
        {
            RaiseStatus("Disconnect Chrome before changing port, attach mode, window size, or Chrome args.");
            return;
        }

        ApplyDraftToOptionsAndSave(draft, applyConnectionSensitive: !_operatorService.IsConnected);
        RaiseStatus("Settings applied and saved.");
    }

    /// <summary>Persists current UI state to disk (called on window close).</summary>
    public void PersistToDisk()
    {
        var draft = CreateDraftFromViewModel();
        McpHostSettingsStore.Save(draft.ToSettingsSnapshot());
    }

    partial void OnEnableListNetworkRequestsToolChanged(bool value) =>
        HandleToolGroupToggle(McpToolGroup.NetworkRequests, value);

    partial void OnEnableListConsoleMessagesToolChanged(bool value) =>
        HandleToolGroupToggle(McpToolGroup.ConsoleMessages, value);

    partial void OnEnableEvaluateToolChanged(bool value) =>
        HandleToolGroupToggle(McpToolGroup.Evaluate, value);

    partial void OnEnableScreenshotToolChanged(bool value) =>
        HandleToolGroupToggle(McpToolGroup.Screenshot, value);

    partial void OnEnableTakeSnapshotToolChanged(bool value) =>
        HandleToolGroupToggle(McpToolGroup.Snapshot, value);

    partial void OnCategoryInputChanged(bool value) =>
        HandleToolGroupToggle(McpToolGroup.Input, value);

    partial void OnCategoryNavigationChanged(bool value) =>
        HandleToolGroupToggle(McpToolGroup.Navigation, value);

    partial void OnCategoryMemoryChanged(bool value) =>
        HandleToolGroupToggle(McpToolGroup.Memory, value);

    partial void OnEnableNetworkCaptureOnConnectChanged(bool value)
    {
        if (_suppressDependencyDialogs)
            return;

        if (value)
        {
            PersistLiveSettings();
            return;
        }

        HandleBehaviorDisable(McpBrowserBehavior.NetworkCdp);
    }

    partial void OnEnableConsoleCaptureOnConnectChanged(bool value)
    {
        if (_suppressDependencyDialogs)
            return;

        if (value)
        {
            PersistLiveSettings();
            return;
        }

        HandleBehaviorDisable(McpBrowserBehavior.ConsoleCdp);
    }

    partial void OnEnableFrameTrackerOnConnectChanged(bool value) => OnBehaviorToggleChanged();

    partial void OnEnableDomTrackerOnConnectChanged(bool value) => OnBehaviorToggleChanged();

    partial void OnEnableBrowserLogCaptureOnConnectChanged(bool value) => OnBehaviorToggleChanged();

    void OnBehaviorToggleChanged()
    {
        if (_suppressDependencyDialogs)
            return;

        PersistLiveSettings();
    }

    void HandleToolGroupToggle(McpToolGroup group, bool wantEnable)
    {
        if (_suppressDependencyDialogs)
            return;

        var draft = CreateDraftFromViewModel();
        McpSettingsDependencyPrompt prompt = wantEnable
            ? McpSettingsDependencies.TryEnableToolGroup(group, draft)
            : McpSettingsDependencies.TryDisableToolGroup(group, draft);

        if (prompt != null)
        {
            var result = MessageBox.Show(
                prompt.Message,
                "Settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                RevertToolGroup(group, !wantEnable);
                return;
            }

            prompt.ApplyOnConfirm(draft);
        }

        ApplyDraftToViewModel(draft);
        PersistLiveSettings();
    }

    void HandleBehaviorDisable(McpBrowserBehavior behavior)
    {
        var draft = CreateDraftFromViewModel();
        var prompt = McpSettingsDependencies.TryDisableBehavior(behavior, draft);
        if (prompt == null)
        {
            PersistLiveSettings();
            return;
        }

        var result = MessageBox.Show(
            prompt.Message,
            "Settings",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            RevertBehavior(behavior, true);
            return;
        }

        prompt.ApplyOnConfirm(draft);
        ApplyDraftToViewModel(draft);
        PersistLiveSettings();
    }

    void RevertToolGroup(McpToolGroup group, bool value)
    {
        _suppressDependencyDialogs = true;
        switch (group)
        {
            case McpToolGroup.NetworkRequests:
                EnableListNetworkRequestsTool = value;
                break;
            case McpToolGroup.ConsoleMessages:
                EnableListConsoleMessagesTool = value;
                break;
            case McpToolGroup.Evaluate:
                EnableEvaluateTool = value;
                break;
            case McpToolGroup.Screenshot:
                EnableScreenshotTool = value;
                break;
            case McpToolGroup.Snapshot:
                EnableTakeSnapshotTool = value;
                break;
            case McpToolGroup.Input:
                CategoryInput = value;
                break;
            case McpToolGroup.Navigation:
                CategoryNavigation = value;
                break;
            case McpToolGroup.Memory:
                CategoryMemory = value;
                break;
        }

        _suppressDependencyDialogs = false;
    }

    void RevertBehavior(McpBrowserBehavior behavior, bool value)
    {
        _suppressDependencyDialogs = true;
        switch (behavior)
        {
            case McpBrowserBehavior.NetworkCdp:
                EnableNetworkCaptureOnConnect = value;
                break;
            case McpBrowserBehavior.ConsoleCdp:
                EnableConsoleCaptureOnConnect = value;
                break;
        }

        _suppressDependencyDialogs = false;
    }

    McpHostOptions CreateDraftFromViewModel()
    {
        var draft = new McpHostOptions();
        CopyToOptions(draft);
        return draft;
    }

    void ApplyDraftToViewModel(McpHostOptions draft)
    {
        _suppressDependencyDialogs = true;
        EnableListNetworkRequestsTool = draft.EnableListNetworkRequestsTool;
        EnableListConsoleMessagesTool = draft.EnableListConsoleMessagesTool;
        EnableEvaluateTool = draft.EnableEvaluateTool;
        EnableScreenshotTool = draft.EnableScreenshotTool;
        EnableTakeSnapshotTool = draft.EnableTakeSnapshotTool;
        EnableNetworkCaptureOnConnect = draft.EnableNetworkCaptureOnConnect;
        EnableConsoleCaptureOnConnect = draft.EnableConsoleCaptureOnConnect;
        CategoryInput = draft.Categories.Input;
        CategoryNavigation = draft.Categories.Navigation;
        CategoryMemory = draft.Categories.Memory;
        _suppressDependencyDialogs = false;
    }

    void LoadFromOptions()
    {
        _suppressDependencyDialogs = true;
        Port = _options.Port;
        Headless = _options.Headless;
        CommandLineArguments = _options.CommandLineArguments ?? "";
        AttachOnly = _options.AttachOnly;
        WindowWidth = _options.WindowWidth;
        WindowHeight = _options.WindowHeight;
        EnableFrameTrackerOnConnect = _options.EnableFrameTrackerOnConnect;
        EnableDomTrackerOnConnect = _options.EnableDomTrackerOnConnect;
        EnableBrowserLogCaptureOnConnect = _options.EnableBrowserLogCaptureOnConnect;
        EnableNetworkCaptureOnConnect = _options.EnableNetworkCaptureOnConnect;
        EnableConsoleCaptureOnConnect = _options.EnableConsoleCaptureOnConnect;
        EnableListNetworkRequestsTool = _options.EnableListNetworkRequestsTool;
        EnableListConsoleMessagesTool = _options.EnableListConsoleMessagesTool;
        EnableEvaluateTool = _options.EnableEvaluateTool;
        EnableScreenshotTool = _options.EnableScreenshotTool;
        EnableTakeSnapshotTool = _options.EnableTakeSnapshotTool;
        CategoryInput = _options.Categories.Input;
        CategoryNavigation = _options.Categories.Navigation;
        CategoryMemory = _options.Categories.Memory;
        McpHttpPort = _options.McpHttpPort;
        McpHttpPath = _options.McpHttpPath ?? "/mcp";
        ConnectChromeOnStartup = _options.ConnectChromeOnStartup;
        _suppressDependencyDialogs = false;
    }

    void CopyToOptions(McpHostOptions target)
    {
        target.Port = Port;
        target.Headless = Headless;
        target.UserDir = _options.UserDir;
        target.IsTempProfile = _options.IsTempProfile;
        target.ActiveProfileId = _options.ActiveProfileId;
        target.ActiveProfileName = _options.ActiveProfileName;
        target.CommandLineArguments = CommandLineArguments;
        target.AttachOnly = AttachOnly;
        target.WindowWidth = WindowWidth;
        target.WindowHeight = WindowHeight;
        target.EnableFrameTrackerOnConnect = EnableFrameTrackerOnConnect;
        target.EnableDomTrackerOnConnect = EnableDomTrackerOnConnect;
        target.EnableBrowserLogCaptureOnConnect = EnableBrowserLogCaptureOnConnect;
        target.EnableNetworkCaptureOnConnect = EnableNetworkCaptureOnConnect;
        target.EnableConsoleCaptureOnConnect = EnableConsoleCaptureOnConnect;
        target.EnableListNetworkRequestsTool = EnableListNetworkRequestsTool;
        target.EnableListConsoleMessagesTool = EnableListConsoleMessagesTool;
        target.EnableEvaluateTool = EnableEvaluateTool;
        target.EnableScreenshotTool = EnableScreenshotTool;
        target.EnableTakeSnapshotTool = EnableTakeSnapshotTool;
        target.McpHttpPort = McpHttpPort;
        target.McpHttpPath = McpHttpPath;
        target.ConnectChromeOnStartup = ConnectChromeOnStartup;
        target.Categories.Input = CategoryInput;
        target.Categories.Navigation = CategoryNavigation;
        target.Categories.Memory = CategoryMemory;
        target.Categories.Emulation = _options.Categories.Emulation;
    }

    void PersistLiveSettings()
    {
        var draft = CreateDraftFromViewModel();
        ApplyDraftToOptionsAndSave(draft, applyConnectionSensitive: false);
    }

    void ApplyDraftToOptionsAndSave(McpHostOptions draft, bool applyConnectionSensitive)
    {
        McpHostSettingsStore.Save(draft.ToSettingsSnapshot());
        if (applyConnectionSensitive)
            _options.CopyFrom(draft);
        else
            _options.ApplyLiveSettingsFrom(draft);
    }

    bool HasConnectionSensitiveChanges(McpHostOptions draft)
    {
        return draft.Port != _options.Port
            || draft.Headless != _options.Headless
            || !string.Equals(draft.CommandLineArguments ?? "", _options.CommandLineArguments ?? "", StringComparison.Ordinal)
            || draft.AttachOnly != _options.AttachOnly
            || draft.WindowWidth != _options.WindowWidth
            || draft.WindowHeight != _options.WindowHeight
            || draft.ConnectChromeOnStartup != _options.ConnectChromeOnStartup
            || draft.McpHttpPort != _options.McpHttpPort
            || !string.Equals(draft.McpHttpPath ?? "/mcp", _options.McpHttpPath ?? "/mcp", StringComparison.Ordinal);
    }

    void RaiseStatus(string message) => StatusChanged?.Invoke(this, message);
}
