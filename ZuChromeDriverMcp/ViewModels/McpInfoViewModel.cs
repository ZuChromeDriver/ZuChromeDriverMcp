using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZuChromeDriverMcp.Core.Configuration;

namespace ZuChromeDriverMcp.ViewModels;

public partial class McpInfoViewModel : ObservableObject
{
    public McpInfoViewModel(McpHostOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        TransportMode = "HTTP (Streamable HTTP)";
        HttpEndpointUrl = _options.GetHttpEndpointUrl();
        SampleClientConfig = BuildSampleConfig();
    }

    readonly McpHostOptions _options;

    public event EventHandler<string> StatusChanged;

    [ObservableProperty]
    private string _transportMode = "";

    [ObservableProperty]
    private string _httpEndpointUrl = "";

    [ObservableProperty]
    private string _sampleClientConfig = "";

    [RelayCommand]
    void CopyUrl()
    {
        Clipboard.SetText(HttpEndpointUrl);
        RaiseStatus("MCP URL copied to clipboard.");
    }

    [RelayCommand]
    void CopyConfig()
    {
        Clipboard.SetText(SampleClientConfig);
        RaiseStatus("Sample MCP config copied to clipboard.");
    }

    string BuildSampleConfig()
    {
        return """
        {
          "mcpServers": {
            "zu-chrome-driver-wpf": {
              "url": "HTTP_ENDPOINT_URL"
            }
          }
        }
        """.Replace("HTTP_ENDPOINT_URL", HttpEndpointUrl, StringComparison.Ordinal);
    }

    void RaiseStatus(string message) => StatusChanged?.Invoke(this, message);
}
