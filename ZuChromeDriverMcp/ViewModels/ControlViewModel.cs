using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZuChromeDriverMcp.Core.Browser;
using ZuChromeDriverMcp.Utils;

namespace ZuChromeDriverMcp.ViewModels;

public partial class ControlViewModel : ObservableObject
{
    readonly McpOperatorService _operatorService;
    readonly McpBrowserContext _browserContext;

    public ControlViewModel(McpOperatorService operatorService, McpBrowserContext browserContext)
    {
        _operatorService = operatorService ?? throw new ArgumentNullException(nameof(operatorService));
        _browserContext = browserContext ?? throw new ArgumentNullException(nameof(browserContext));
        _browserContext.ConnectionStateChanged += OnConnectionStateChanged;
        IsConnected = _operatorService.IsConnected;
    }

    public event EventHandler<string> StatusChanged;

    public ObservableCollection<PageRowViewModel> Pages { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(NavigateCommand))]
    [NotifyCanExecuteChangedFor(nameof(ScreenshotCommand))]
    [NotifyCanExecuteChangedFor(nameof(RefreshPagesCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectPageCommand))]
    private bool _isConnected;

    [ObservableProperty]
    private string _navigateUrl = "https://example.com";

    [ObservableProperty]
    private PageRowViewModel _selectedPage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenLastScreenshotFolderCommand))]
    private string _lastScreenshotPath = "";

    [RelayCommand(CanExecute = nameof(CanConnect))]
    async Task ConnectAsync()
    {
        try
        {
            await _operatorService.ConnectAsync().ConfigureAwait(true);
            IsConnected = _operatorService.IsConnected;
            RaiseStatus("Connected to Chrome.");
            await RefreshPagesAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _operatorService.ReportError(ex.Message);
            RaiseStatus(ex.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    async Task DisconnectAsync()
    {
        try
        {
            await _operatorService.DisconnectAsync().ConfigureAwait(true);
            IsConnected = _operatorService.IsConnected;
            Pages.Clear();
            SelectedPage = null;
            RaiseStatus("Disconnected from Chrome.");
        }
        catch (Exception ex)
        {
            _operatorService.ReportError(ex.Message);
            RaiseStatus(ex.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(CanOperate))]
    async Task NavigateAsync()
    {
        try
        {
            await _operatorService.NavigateAsync(NavigateUrl).ConfigureAwait(true);
            RaiseStatus($"Navigated to {NavigateUrl}.");
            await RefreshPagesAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _operatorService.ReportError(ex.Message);
            RaiseStatus(ex.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(CanOperate))]
    async Task ScreenshotAsync()
    {
        try
        {
            var path = await _operatorService.TakeScreenshotAsync().ConfigureAwait(true);
            LastScreenshotPath = path;
            RaiseStatus($"Screenshot saved: {path}");
        }
        catch (Exception ex)
        {
            _operatorService.ReportError(ex.Message);
            RaiseStatus(ex.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(CanOperate))]
    async Task RefreshPagesAsync()
    {
        try
        {
            var pages = await _operatorService.ListPagesAsync().ConfigureAwait(true);
            Pages.Clear();
            foreach (var page in pages)
            {
                Pages.Add(new PageRowViewModel
                {
                    PageId = page.PageId,
                    Url = page.Url,
                    Title = page.Title,
                    IsSelected = page.IsSelected,
                });
            }

            SelectedPage = Pages.FirstOrDefault(p => p.IsSelected) ?? Pages.FirstOrDefault();
            RaiseStatus($"Loaded {Pages.Count} page(s).");
        }
        catch (Exception ex)
        {
            _operatorService.ReportError(ex.Message);
            RaiseStatus(ex.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(CanSelectPage))]
    async Task SelectPageAsync()
    {
        if (SelectedPage == null)
            return;

        try
        {
            await _operatorService.SelectPageAsync(SelectedPage.PageId).ConfigureAwait(true);
            await RefreshPagesAsync().ConfigureAwait(true);
            RaiseStatus($"Selected page {SelectedPage.PageId}.");
        }
        catch (Exception ex)
        {
            _operatorService.ReportError(ex.Message);
            RaiseStatus(ex.Message);
        }
    }

    bool CanConnect() => !IsConnected;

    bool CanDisconnect() => IsConnected;

    bool CanOperate() => IsConnected;

    bool CanSelectPage() => IsConnected && SelectedPage != null;

    [RelayCommand(CanExecute = nameof(CanOpenLastScreenshotFolder))]
    void OpenLastScreenshotFolder()
    {
        var path = LastScreenshotPath;
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

    bool CanOpenLastScreenshotFolder() => !string.IsNullOrWhiteSpace(LastScreenshotPath);

    void OnConnectionStateChanged(object sender, EventArgs e)
    {
        var isConnected = _operatorService.IsConnected;
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null)
            return;

        if (dispatcher.CheckAccess())
        {
            ApplyConnectionState(isConnected);
            return;
        }

        dispatcher.InvokeAsync(() => ApplyConnectionState(isConnected));
    }

    void ApplyConnectionState(bool isConnected)
    {
        IsConnected = isConnected;
        if (isConnected)
            return;

        Pages.Clear();
        SelectedPage = null;
    }

    void RaiseStatus(string message) => StatusChanged?.Invoke(this, message);
}
