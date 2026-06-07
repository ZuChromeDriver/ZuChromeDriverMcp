using CommunityToolkit.Mvvm.ComponentModel;

namespace ZuChromeDriverMcp.ViewModels;

public partial class PageRowViewModel : ObservableObject
{
    [ObservableProperty]
    private int _pageId;

    [ObservableProperty]
    private string _url = "";

    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private bool _isSelected;
}
