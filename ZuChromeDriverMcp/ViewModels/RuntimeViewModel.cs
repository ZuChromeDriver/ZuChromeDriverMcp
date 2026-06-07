using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZuChromeDriverMcp.Core.Browser;
using ZuChromeDriverMcp.Utils;

namespace ZuChromeDriverMcp.ViewModels;

public partial class RuntimeViewModel : ObservableObject
{
    readonly McpRuntimeMonitor _runtimeMonitor;

    public RuntimeViewModel(McpRuntimeMonitor runtimeMonitor)
    {
        _runtimeMonitor = runtimeMonitor ?? throw new ArgumentNullException(nameof(runtimeMonitor));
        _runtimeMonitor.SnapshotChanged += OnSnapshotChanged;
        ApplySnapshot(_runtimeMonitor.CurrentSnapshot);
    }

    public event EventHandler<string> StatusChanged;

    public ObservableCollection<string> RecentConsole { get; } = new();

    public ObservableCollection<string> RecentNetwork { get; } = new();

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private int _chromePort;

    [ObservableProperty]
    private string _activePageUrl = "";

    [ObservableProperty]
    private int _networkCount;

    [ObservableProperty]
    private int _consoleCount;

    [ObservableProperty]
    private string _lastError = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenLastArtifactFolderCommand))]
    private string _lastArtifactPath = "";

    [RelayCommand]
    async Task RefreshAsync()
    {
        await _runtimeMonitor.RefreshAsync().ConfigureAwait(true);
        RaiseStatus("Runtime refreshed.");
    }

    [RelayCommand(CanExecute = nameof(CanOpenLastArtifactFolder))]
    void OpenLastArtifactFolder()
    {
        var path = LastArtifactPath;
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            ExplorerPathOpener.OpenInExplorer(path);
        }
        catch (Exception ex)
        {
            RaiseStatus(ex.Message);
        }
    }

    bool CanOpenLastArtifactFolder() => !string.IsNullOrWhiteSpace(LastArtifactPath);

    void OnSnapshotChanged(object sender, McpRuntimeSnapshot snapshot)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null)
            return;

        if (dispatcher.CheckAccess())
        {
            ApplySnapshot(snapshot);
            return;
        }

        dispatcher.InvokeAsync(() => ApplySnapshot(snapshot));
    }

    void ApplySnapshot(McpRuntimeSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        IsConnected = snapshot.IsConnected;
        ChromePort = snapshot.ChromePort;
        ActivePageUrl = snapshot.ActivePageUrl ?? "";
        NetworkCount = snapshot.NetworkCount;
        ConsoleCount = snapshot.ConsoleCount;
        LastError = snapshot.LastError ?? "";
        LastArtifactPath = snapshot.LastArtifactPath ?? "";

        RecentConsole.Clear();
        foreach (var entry in snapshot.RecentConsole)
            RecentConsole.Add($"[{entry.Type}] {entry.Text}");

        RecentNetwork.Clear();
        foreach (var entry in snapshot.RecentNetwork)
            RecentNetwork.Add($"{entry.Method} {entry.Url}");
    }

    void RaiseStatus(string message) => StatusChanged?.Invoke(this, message);
}
