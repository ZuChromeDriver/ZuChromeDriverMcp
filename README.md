# ZuChromeDriverMcp

MCP-сервер для автоматизации Chrome из .NET: [Model Context Protocol](https://modelcontextprotocol.io/) поверх **[ZuChromeDriver](https://github.com/ToCSharp/ZuChromeDriver)**, без PuppeteerSharp и без дублирования CDP-клиента.

Два MCP-хоста поверх общего **`ZuChromeDriverMcp.Core`**:

| Хост | Назначение |
|------|------------|
| **`ZuChromeDriverMcp`** (WPF) | **Рекомендуется** — UI с настройками, профилями и рантайм-панелью; HTTP MCP на localhost; headless stdio через `--stdio` |
| **`ZuChromeDriverMcp.Host`** | Консольный stdio-хост без UI — минимальный вариант |

## Возможности

Навигация, скрипты, скриншоты, вкладки, a11y-snapshot с uid, клик/ввод, сеть и консоль, heap snapshot.

### Профили Chrome

Список, подключение и отключение профилей Chrome (`list_chrome_profiles`, `connect_chrome`, `disconnect_chrome`).

## TODO

- **Lighthouse** — `lighthouse_audit` (a11y, SEO, best practices); референс: [chrome-devtools-mcp](https://github.com/ChromeDevTools/chrome-devtools-mcp).
- **Performance** — `performance_start_trace`, `performance_stop_trace`, `performance_analyze_insight` (CDP `Tracing` + Performance Insights).

## Требования

- .NET 10 SDK
- Google Chrome / Chromium с remote debugging

## Запуск MCP-сервера

### WPF-хост

**Рекомендуется** — удобнее для повседневной работы и подключения в Cursor: настройки, профили, рантайм-панель.

GUI + HTTP MCP (по умолчанию):

```bash
dotnet run --project ZuChromeDriverMcp/ZuChromeDriverMcp.csproj
```

MCP endpoint: `http://127.0.0.1:5100/mcp` (порт и путь настраиваются в UI или через env).

Headless stdio (без окна, как Host):

```bash
dotnet run --project ZuChromeDriverMcp/ZuChromeDriverMcp.csproj -- --stdio
```

В GUI: вкладки **Настройки**, **Профили**, **Управление** (Navigate / Screenshot / вкладки), **Рантайм**, **MCP**.

### Профили Chrome (WPF)

Каталог профилей: `{exe_dir}/Profiles/` (рядом с исполняемым файлом). Список и выбранный профиль сохраняются в `Profiles/profiles.json`.

По умолчанию создаются:

| Профиль | Тип | Путь |
|---------|-----|------|
| **Temp** | Временный | `%TEMP%` — удаляется при закрытии Chrome |
| **Profile1** | Папка | `{exe_dir}/Profiles/Profile1` |
| **Profile2** | Папка | `{exe_dir}/Profiles/Profile2` |

На вкладке **Профили** можно выбрать активный профиль, добавить папочный профиль (подпапка в `Profiles/`) или профиль с произвольным путём. Выбор сохраняется для следующих запусков.

### Подключение в Cursor

```json
{
  "mcpServers": {
    "zu-chrome-driver-wpf": {
      "url": "http://127.0.0.1:5100/mcp"
    }
  }
}
```

### Host (stdio) — второй вариант

Второй MCP-хост: консольный stdio без UI — минимальный вариант для скриптов и CI:

```bash
dotnet run --project ZuChromeDriverMcp.Host/ZuChromeDriverMcp.Host.csproj
```

Опционально — отключить категорию tools (как в chrome-devtools-mcp):

```bash
dotnet run --project ZuChromeDriverMcp.Host/ZuChromeDriverMcp.Host.csproj -- --category-network=false
```

Логи пишутся в **stderr**; stdout занят JSON-RPC MCP — не перенаправляйте его в консоль вручную.

## Типичный сценарий агента

1. `list_pages` → выбрать вкладку  
2. `select_page` с `pageId`  
3. `navigate` или `new_page`  
4. `take_snapshot` → получить **uid** элементов  
5. `click` / `fill` с `uid` (или `selector` для простых случаев)  
6. При необходимости — `list_console_messages` / `list_network_requests`

## Структура решения

| Проект | Назначение |
|--------|------------|
| **`ZuChromeDriverMcp.Host`** | Console, MCP stdio, DI, lifecycle Chrome |
| **`ZuChromeDriverMcp.Core`** | Tools, snapshot, pages, collectors, mutex |
| **`ZuChromeDriverMcp`** | WPF — второй MCP-хост: HTTP + `--stdio`, MVVM UI, рантайм-панель |
| **`ZuChromeDriver`** *(NuGet)* | Драйвер CDP + WebDriver-фасад |
| **`ChromeDevToolsClient`** *(NuGet, транзитивно)* | WebSocket JSON-RPC к CDP |

## Документация

| Документ | Содержание |
|----------|------------|
| [ARCHITECTURE.md](ARCHITECTURE.md) | Архитектура MCP-слоя, потоки данных, компоненты |

## Принципы

- **CDP только через `ChromeDevToolsClient`** — MCP не добавляет свой транспорт.
- **Автоматизация через `ZuChromeDriver`** — навигация, скрипты, клики не дублируются в MCP.
- **MCP-слой** — protocol, mapping tools, snapshot uid, collectors.

## Лицензия

Следует лицензии **ZuChromeDriver** (Apache 2.0) для кода драйвера; MCP-слой — по лицензии репозитория.
