using CommunityToolkit.Mvvm.ComponentModel;

namespace ZuChromeDriverMcp.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public MainWindowViewModel(
        SettingsViewModel settings,
        ProfilesViewModel profiles,
        ControlViewModel control,
        RuntimeViewModel runtime,
        McpInfoViewModel mcpInfo)
    {
        Settings = settings;
        Profiles = profiles;
        Control = control;
        Runtime = runtime;
        McpInfo = mcpInfo;

        Control.StatusChanged += OnChildStatusChanged;
        Settings.StatusChanged += OnChildStatusChanged;
        Profiles.StatusChanged += OnChildStatusChanged;
        Runtime.StatusChanged += OnChildStatusChanged;
        McpInfo.StatusChanged += OnChildStatusChanged;
    }

    public SettingsViewModel Settings { get; }

    public ProfilesViewModel Profiles { get; }

    public ControlViewModel Control { get; }

    public RuntimeViewModel Runtime { get; }

    public McpInfoViewModel McpInfo { get; }

    [ObservableProperty]
    private string _statusMessage = "Ready.";

    void OnChildStatusChanged(object sender, string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
            StatusMessage = message;
    }
}
