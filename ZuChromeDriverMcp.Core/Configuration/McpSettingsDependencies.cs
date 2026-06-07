using ZuChromeDriverMcp.Core.Tools;

namespace ZuChromeDriverMcp.Core.Configuration;

public sealed class McpSettingsDependencyPrompt
{
    public string Message { get; init; }

    public Action<McpHostOptions> ApplyOnConfirm { get; init; }

    public static McpSettingsDependencyPrompt None => null;
}

public static class McpSettingsDependencies
{
    public static McpSettingsDependencyPrompt TryEnableToolGroup(McpToolGroup group, McpHostOptions draft)
    {
        if (draft == null)
            throw new ArgumentNullException(nameof(draft));

        switch (group)
        {
            case McpToolGroup.NetworkRequests:
                if (!draft.EnableNetworkCaptureOnConnect)
                {
                    return new McpSettingsDependencyPrompt
                    {
                        Message =
                            "Tool \"list_network_requests\" requires Network (CDP) capture.\n\nEnable Network (CDP) in browser behavior?",
                        ApplyOnConfirm = o =>
                        {
                            o.EnableNetworkCaptureOnConnect = true;
                            o.EnableListNetworkRequestsTool = true;
                        },
                    };
                }

                draft.EnableListNetworkRequestsTool = true;
                return McpSettingsDependencyPrompt.None;

            case McpToolGroup.ConsoleMessages:
                if (!draft.EnableConsoleCaptureOnConnect)
                {
                    return new McpSettingsDependencyPrompt
                    {
                        Message =
                            "Tool \"list_console_messages\" requires Console (CDP) capture.\n\nEnable Console (CDP) in browser behavior?",
                        ApplyOnConfirm = o =>
                        {
                            o.EnableConsoleCaptureOnConnect = true;
                            o.EnableListConsoleMessagesTool = true;
                        },
                    };
                }

                draft.EnableListConsoleMessagesTool = true;
                return McpSettingsDependencyPrompt.None;

            case McpToolGroup.Evaluate:
                draft.EnableEvaluateTool = true;
                return McpSettingsDependencyPrompt.None;

            case McpToolGroup.Screenshot:
                draft.EnableScreenshotTool = true;
                return McpSettingsDependencyPrompt.None;

            case McpToolGroup.Snapshot:
                draft.EnableTakeSnapshotTool = true;
                return McpSettingsDependencyPrompt.None;

            case McpToolGroup.Input:
                draft.Categories.Input = true;
                return McpSettingsDependencyPrompt.None;

            case McpToolGroup.Navigation:
                draft.Categories.Navigation = true;
                return McpSettingsDependencyPrompt.None;

            case McpToolGroup.Memory:
                draft.Categories.Memory = true;
                return McpSettingsDependencyPrompt.None;

            default:
                return McpSettingsDependencyPrompt.None;
        }
    }

    public static McpSettingsDependencyPrompt TryDisableToolGroup(McpToolGroup group, McpHostOptions draft)
    {
        if (draft == null)
            throw new ArgumentNullException(nameof(draft));

        switch (group)
        {
            case McpToolGroup.NetworkRequests:
                draft.EnableListNetworkRequestsTool = false;
                return McpSettingsDependencyPrompt.None;

            case McpToolGroup.ConsoleMessages:
                draft.EnableListConsoleMessagesTool = false;
                return McpSettingsDependencyPrompt.None;

            case McpToolGroup.Evaluate:
                draft.EnableEvaluateTool = false;
                return McpSettingsDependencyPrompt.None;

            case McpToolGroup.Screenshot:
                draft.EnableScreenshotTool = false;
                return McpSettingsDependencyPrompt.None;

            case McpToolGroup.Snapshot:
                draft.EnableTakeSnapshotTool = false;
                return McpSettingsDependencyPrompt.None;

            case McpToolGroup.Input:
                draft.Categories.Input = false;
                return McpSettingsDependencyPrompt.None;

            case McpToolGroup.Navigation:
                draft.Categories.Navigation = false;
                return McpSettingsDependencyPrompt.None;

            case McpToolGroup.Memory:
                draft.Categories.Memory = false;
                return McpSettingsDependencyPrompt.None;

            default:
                return McpSettingsDependencyPrompt.None;
        }
    }

    public static McpSettingsDependencyPrompt TryDisableBehavior(McpBrowserBehavior behavior, McpHostOptions draft)
    {
        if (draft == null)
            throw new ArgumentNullException(nameof(draft));

        switch (behavior)
        {
            case McpBrowserBehavior.NetworkCdp:
                if (draft.EnableListNetworkRequestsTool)
                {
                    return new McpSettingsDependencyPrompt
                    {
                        Message =
                            "Network requests tool depends on Network (CDP) capture.\n\nDisable Network requests tool and Network (CDP)?",
                        ApplyOnConfirm = o =>
                        {
                            o.EnableListNetworkRequestsTool = false;
                            o.EnableNetworkCaptureOnConnect = false;
                        },
                    };
                }

                draft.EnableNetworkCaptureOnConnect = false;
                return McpSettingsDependencyPrompt.None;

            case McpBrowserBehavior.ConsoleCdp:
                if (draft.EnableListConsoleMessagesTool)
                {
                    return new McpSettingsDependencyPrompt
                    {
                        Message =
                            "Console messages tool depends on Console (CDP) capture.\n\nDisable Console messages tool and Console (CDP)?",
                        ApplyOnConfirm = o =>
                        {
                            o.EnableListConsoleMessagesTool = false;
                            o.EnableConsoleCaptureOnConnect = false;
                        },
                    };
                }

                draft.EnableConsoleCaptureOnConnect = false;
                return McpSettingsDependencyPrompt.None;

            case McpBrowserBehavior.FrameTracker:
                draft.EnableFrameTrackerOnConnect = false;
                return McpSettingsDependencyPrompt.None;

            case McpBrowserBehavior.DomTracker:
                draft.EnableDomTrackerOnConnect = false;
                return McpSettingsDependencyPrompt.None;

            case McpBrowserBehavior.BrowserLog:
                draft.EnableBrowserLogCaptureOnConnect = false;
                return McpSettingsDependencyPrompt.None;

            default:
                return McpSettingsDependencyPrompt.None;
        }
    }

    public static McpSettingsDependencyPrompt TryEnableBehavior(McpBrowserBehavior behavior, McpHostOptions draft)
    {
        if (draft == null)
            throw new ArgumentNullException(nameof(draft));

        switch (behavior)
        {
            case McpBrowserBehavior.NetworkCdp:
                draft.EnableNetworkCaptureOnConnect = true;
                return McpSettingsDependencyPrompt.None;

            case McpBrowserBehavior.ConsoleCdp:
                draft.EnableConsoleCaptureOnConnect = true;
                return McpSettingsDependencyPrompt.None;

            case McpBrowserBehavior.FrameTracker:
                draft.EnableFrameTrackerOnConnect = true;
                return McpSettingsDependencyPrompt.None;

            case McpBrowserBehavior.DomTracker:
                draft.EnableDomTrackerOnConnect = true;
                return McpSettingsDependencyPrompt.None;

            case McpBrowserBehavior.BrowserLog:
                draft.EnableBrowserLogCaptureOnConnect = true;
                return McpSettingsDependencyPrompt.None;

            default:
                return McpSettingsDependencyPrompt.None;
        }
    }
}
