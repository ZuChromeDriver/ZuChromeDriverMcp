# ZuChromeDriverMcp

MCP server for Chrome automation from .NET: [Model Context Protocol](https://modelcontextprotocol.io/) on top of **[ZuChromeDriver](https://github.com/ZuChromeDriver/ZuChromeDriver)**, without PuppeteerSharp and without duplicating the CDP client.

Two MCP hosts on top of shared **`ZuChromeDriverMcp.Core`**:

| Host | Purpose |
|------|---------|
| **`ZuChromeDriverMcp`** (WPF) | **Recommended** — UI with settings, profiles, and runtime panel; HTTP MCP on localhost; headless stdio via `--stdio` |
| **`ZuChromeDriverMcp.Host`** | Console stdio host without UI — minimal option |

## Features

Navigation, scripts, screenshots, tabs, a11y snapshot with uid, click/input, network and console, heap snapshot.

### Chrome Profiles

List, connect, and disconnect Chrome profiles (`list_chrome_profiles`, `connect_chrome`, `disconnect_chrome`).

## TODO

- **Lighthouse** — `lighthouse_audit` (a11y, SEO, best practices); reference: [chrome-devtools-mcp](https://github.com/ChromeDevTools/chrome-devtools-mcp).
- **Performance** — `performance_start_trace`, `performance_stop_trace`, `performance_analyze_insight` (CDP `Tracing` + Performance Insights).

## Requirements

- .NET 10 SDK
- Google Chrome / Chromium with remote debugging

## Running the MCP Server

### WPF Host

**Recommended** — more convenient for day-to-day use and connecting in Cursor: settings, profiles, runtime panel.

GUI + HTTP MCP (default):

```bash
dotnet run --project ZuChromeDriverMcp/ZuChromeDriverMcp.csproj
```

MCP endpoint: `http://127.0.0.1:5100/mcp` (port and path are configurable in the UI or via env).

Headless stdio (no window, like Host):

```bash
dotnet run --project ZuChromeDriverMcp/ZuChromeDriverMcp.csproj -- --stdio
```

In the GUI: **Settings**, **Profiles**, **Control** (Navigate / Screenshot / tabs), **Runtime**, **MCP** tabs.

### Chrome Profiles (WPF)

Profile directory: `{exe_dir}/Profiles/` (next to the executable). The list and selected profile are saved in `Profiles/profiles.json`.

Created by default:

| Profile | Type | Path |
|---------|------|------|
| **Temp** | Temporary | `%TEMP%` — deleted when Chrome closes |
| **Profile1** | Folder | `{exe_dir}/Profiles/Profile1` |
| **Profile2** | Folder | `{exe_dir}/Profiles/Profile2` |

On the **Profiles** tab you can select the active profile, add a folder profile (subfolder in `Profiles/`), or a profile with a custom path. The selection is saved for future runs.

### Connecting in Cursor

```json
{
  "mcpServers": {
    "zu-chrome-driver-wpf": {
      "url": "http://127.0.0.1:5100/mcp"
    }
  }
}
```

### Host (stdio) — Alternative

Second MCP host: console stdio without UI — minimal option for scripts and CI:

```bash
dotnet run --project ZuChromeDriverMcp.Host/ZuChromeDriverMcp.Host.csproj
```

Optionally — disable a tool category (as in chrome-devtools-mcp):

```bash
dotnet run --project ZuChromeDriverMcp.Host/ZuChromeDriverMcp.Host.csproj -- --category-network=false
```

Logs are written to **stderr**; stdout is reserved for JSON-RPC MCP — do not redirect it to the console manually.

## Typical Agent Workflow

1. `list_pages` → select a tab  
2. `select_page` with `pageId`  
3. `navigate` or `new_page`  
4. `take_snapshot` → get element **uid**  
5. `click` / `fill` with `uid` (or `selector` for simple cases)  
6. If needed — `list_console_messages` / `list_network_requests`

## Solution Structure

| Project | Purpose |
|---------|---------|
| **`ZuChromeDriverMcp.Host`** | Console, MCP stdio, DI, Chrome lifecycle |
| **`ZuChromeDriverMcp.Core`** | Tools, snapshot, pages, collectors, mutex |
| **`ZuChromeDriverMcp`** | WPF — second MCP host: HTTP + `--stdio`, MVVM UI, runtime panel |
| **`ZuChromeDriver`** *(NuGet)* | CDP driver + WebDriver facade |
| **`ChromeDevToolsClient`** *(NuGet, transitive)* | WebSocket JSON-RPC to CDP |

## Documentation

| Document | Contents |
|----------|----------|
| [ARCHITECTURE.md](ARCHITECTURE.md) | MCP layer architecture, data flows, components |

## Principles

- **CDP only through `ChromeDevToolsClient`** — MCP does not add its own transport.
- **Automation through `ZuChromeDriver`** — navigation, scripts, clicks are not duplicated in MCP.
- **MCP layer** — protocol, tool mapping, snapshot uid, collectors.

## License

Follows the **ZuChromeDriver** license (Apache 2.0) for driver code; MCP layer — per repository license.
