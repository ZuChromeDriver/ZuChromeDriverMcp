using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ZuChromeDriverMcp.Core.Browser;
using ZuChromeDriverMcp.Core.Configuration;
using ZuChromeDriverMcp.Core.Concurrency;
using ZuChromeDriverMcp.Core.Responses;

namespace ZuChromeDriverMcp.Core.Tools;

[McpServerToolType]
public sealed class ChromeTools
{
    readonly McpBrowserContext _context;
    readonly McpHostOptions _options;
    readonly ChromeProfileService _profileService;
    readonly McpOperatorService _operatorService;
    readonly SingleFlightLock _singleFlightLock;

    public ChromeTools(
        McpBrowserContext context,
        McpHostOptions options,
        ChromeProfileService profileService,
        McpOperatorService operatorService,
        SingleFlightLock singleFlightLock)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _operatorService = operatorService ?? throw new ArgumentNullException(nameof(operatorService));
        _singleFlightLock = singleFlightLock ?? throw new ArgumentNullException(nameof(singleFlightLock));
    }

    [McpServerTool(Name = "list_chrome_profiles", ReadOnly = true, Destructive = false)]
    [Description("Lists saved Chrome profiles from Profiles/profiles.json next to the executable.")]
    public Task<CallToolResult> ListChromeProfiles(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = new McpResponse();

        try
        {
            _profileService.Reload();
            var profiles = _profileService.Profiles.Select(p => new
            {
                id = p.Id,
                name = p.Name,
                kind = p.Kind.ToString(),
                path = p.GetDisplayPath(),
                isBuiltIn = p.IsBuiltIn,
                isSelected = string.Equals(p.Id, _profileService.SelectedProfileId, StringComparison.OrdinalIgnoreCase),
            }).ToList();

            response.AppendLine(JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true }));
            response.AppendLine($"Profiles root: {ChromeProfilePaths.GetProfilesRoot()}");
        }
        catch (Exception ex)
        {
            response.SetError(ex);
        }

        return Task.FromResult(response.ToCallToolResult());
    }

    [McpServerTool(Name = "connect_chrome", ReadOnly = false, Destructive = false)]
    [Description("Launches or attaches Chrome using a saved profile or explicit profile settings. Reconnects if already connected.")]
    public async Task<CallToolResult> ConnectChrome(
        [Description("Saved profile name or id (Temp, Profile1, Profile2, etc.)")] string profile = null,
        [Description("Explicit Chrome user-data-dir path; overrides profile when set")] string userDir = null,
        [Description("Use a temporary profile deleted on close")] bool? isTempProfile = null,
        [Description("Run Chrome headless")] bool? headless = null,
        [Description("Attach only to an existing Chrome on the configured port")] bool? attachOnly = null,
        CancellationToken cancellationToken = default)
    {
        using (await _singleFlightLock.AcquireAsync(cancellationToken).ConfigureAwait(false))
        {
            var response = new McpResponse();

            try
            {
                if (_context.IsConnected)
                    await _operatorService.DisconnectAsync(cancellationToken).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(profile))
                    _profileService.ApplyProfileByNameOrId(profile, _options);
                else if (!string.IsNullOrWhiteSpace(userDir) || isTempProfile == true)
                    _profileService.ApplyDirectProfile(_options, userDir, isTempProfile ?? false);
                else if (string.IsNullOrWhiteSpace(_options.UserDir))
                    _profileService.ApplySelectedTo(_options);

                if (headless.HasValue)
                    _options.Headless = headless.Value;
                if (attachOnly.HasValue)
                    _options.AttachOnly = attachOnly.Value;

                await _operatorService.ConnectAsync(cancellationToken).ConfigureAwait(false);

                var activeProfile = string.IsNullOrWhiteSpace(_options.ActiveProfileName)
                    ? "direct"
                    : _options.ActiveProfileName;
                var userDataDir = _options.IsTempProfile
                    ? "(temp — deleted on close)"
                    : (_options.UserDir ?? "(default)");
                response.AppendLine($"Connected to Chrome on port {_options.Port}.");
                response.AppendLine($"Profile: {activeProfile}");
                response.AppendLine($"User data dir: {userDataDir}");
            }
            catch (Exception ex)
            {
                response.SetError(ex);
            }

            return response.ToCallToolResult();
        }
    }

    [McpServerTool(Name = "disconnect_chrome", ReadOnly = false, Destructive = true)]
    [Description("Closes the Chrome instance launched or attached by this MCP host.")]
    public async Task<CallToolResult> DisconnectChrome(CancellationToken cancellationToken = default)
    {
        using (await _singleFlightLock.AcquireAsync(cancellationToken).ConfigureAwait(false))
        {
            var response = new McpResponse();

            try
            {
                if (!_context.IsConnected)
                {
                    response.AppendLine("Chrome is not connected.");
                    return response.ToCallToolResult();
                }

                await _operatorService.DisconnectAsync(cancellationToken).ConfigureAwait(false);
                response.AppendLine("Disconnected from Chrome.");
            }
            catch (Exception ex)
            {
                response.SetError(ex);
            }

            return response.ToCallToolResult();
        }
    }
}
