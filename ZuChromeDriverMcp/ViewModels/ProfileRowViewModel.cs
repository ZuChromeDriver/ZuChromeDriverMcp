using ZuChromeDriverMcp.Core.Configuration;

namespace ZuChromeDriverMcp.ViewModels;

public sealed class ProfileRowViewModel
{
    public ProfileRowViewModel(ChromeProfileEntry entry, bool isSelected)
    {
        Entry = entry ?? throw new ArgumentNullException(nameof(entry));
        IsSelected = isSelected;
    }

    public ChromeProfileEntry Entry { get; }

    public string Id => Entry.Id;

    public string Name => Entry.Name;

    public string KindDisplay => Entry.Kind switch
    {
        ChromeProfileKind.Temp => "Временный",
        ChromeProfileKind.Folder => "Папка Profiles",
        ChromeProfileKind.CustomPath => "Произвольный путь",
        _ => Entry.Kind.ToString(),
    };

    public string PathDisplay => Entry.GetDisplayPath();

    public bool IsBuiltIn => Entry.IsBuiltIn;

    public bool IsSelected { get; }
}
