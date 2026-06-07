using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ZuChromeDriverMcp.Core.Browser;
using ZuChromeDriverMcp.Core.Configuration;

namespace ZuChromeDriverMcp.ViewModels;

public partial class ProfilesViewModel : ObservableObject
{
    readonly McpHostOptions _options;
    readonly McpOperatorService _operatorService;
    readonly ChromeProfileService _profileService;

    public ProfilesViewModel(
        McpHostOptions options,
        McpOperatorService operatorService,
        ChromeProfileService profileService)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _operatorService = operatorService ?? throw new ArgumentNullException(nameof(operatorService));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        ProfilesRoot = ChromeProfilePaths.GetProfilesRoot();
        ReloadProfiles();
    }

    public event EventHandler<string> StatusChanged;

    public ObservableCollection<ProfileRowViewModel> Profiles { get; } = new();

    [ObservableProperty]
    private ProfileRowViewModel _selectedProfile;

    [ObservableProperty]
    private string _profilesRoot;

    [ObservableProperty]
    private string _newFolderProfileName = "Profile3";

    [ObservableProperty]
    private string _newCustomProfileName = "Custom";

    [ObservableProperty]
    private string _newCustomPath = "";

    [RelayCommand]
    void RefreshProfiles()
    {
        ReloadProfiles();
        RaiseStatus("Profiles refreshed.");
    }

    [RelayCommand]
    void ApplySelectedProfile()
    {
        if (_operatorService.IsConnected)
        {
            RaiseStatus("Disconnect Chrome before changing profile.");
            return;
        }

        if (SelectedProfile == null)
        {
            RaiseStatus("Select a profile first.");
            return;
        }

        try
        {
            _profileService.SelectProfile(SelectedProfile.Id, _options);
            ReloadProfiles();
            RaiseStatus($"Profile '{SelectedProfile.Name}' applied and saved for next startup.");
        }
        catch (Exception ex)
        {
            RaiseStatus(ex.Message);
        }
    }

    [RelayCommand]
    void AddFolderProfile()
    {
        if (string.IsNullOrWhiteSpace(NewFolderProfileName))
        {
            RaiseStatus("Enter a profile name.");
            return;
        }

        try
        {
            var entry = _profileService.AddFolderProfile(NewFolderProfileName, NewFolderProfileName);
            ReloadProfiles();
            SelectedProfile = Profiles.FirstOrDefault(p => string.Equals(p.Id, entry.Id, StringComparison.Ordinal));
            RaiseStatus($"Folder profile '{entry.Name}' added.");
        }
        catch (Exception ex)
        {
            RaiseStatus(ex.Message);
        }
    }

    [RelayCommand]
    void BrowseCustomPath()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Chrome user-data directory",
            InitialDirectory = string.IsNullOrWhiteSpace(NewCustomPath)
                ? ProfilesRoot
                : NewCustomPath,
        };

        if (dialog.ShowDialog() == true)
            NewCustomPath = dialog.FolderName;
    }

    [RelayCommand]
    void AddCustomProfile()
    {
        if (string.IsNullOrWhiteSpace(NewCustomProfileName))
        {
            RaiseStatus("Enter a profile name.");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewCustomPath))
        {
            RaiseStatus("Select a folder path.");
            return;
        }

        try
        {
            var entry = _profileService.AddCustomProfile(NewCustomProfileName, NewCustomPath);
            ReloadProfiles();
            SelectedProfile = Profiles.FirstOrDefault(p => string.Equals(p.Id, entry.Id, StringComparison.Ordinal));
            RaiseStatus($"Custom profile '{entry.Name}' added.");
        }
        catch (Exception ex)
        {
            RaiseStatus(ex.Message);
        }
    }

    [RelayCommand]
    void RemoveSelectedProfile()
    {
        if (SelectedProfile == null)
        {
            RaiseStatus("Select a profile to remove.");
            return;
        }

        if (SelectedProfile.IsBuiltIn)
        {
            RaiseStatus("Built-in profiles cannot be removed.");
            return;
        }

        if (_operatorService.IsConnected)
        {
            RaiseStatus("Disconnect Chrome before removing a profile.");
            return;
        }

        try
        {
            var name = SelectedProfile.Name;
            _profileService.RemoveProfile(SelectedProfile.Id);
            ReloadProfiles();
            RaiseStatus($"Profile '{name}' removed.");
        }
        catch (Exception ex)
        {
            RaiseStatus(ex.Message);
        }
    }

    void ReloadProfiles()
    {
        _profileService.Reload();
        Profiles.Clear();
        var selectedId = _profileService.SelectedProfileId;
        foreach (var entry in _profileService.Profiles)
            Profiles.Add(new ProfileRowViewModel(entry, string.Equals(entry.Id, selectedId, StringComparison.Ordinal)));

        SelectedProfile = Profiles.FirstOrDefault(p => p.IsSelected) ?? Profiles.FirstOrDefault();
        ProfilesRoot = ChromeProfilePaths.GetProfilesRoot();
    }

    void RaiseStatus(string message) => StatusChanged?.Invoke(this, message);
}
