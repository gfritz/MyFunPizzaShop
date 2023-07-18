# MyFunPizzaShop

## Configuration

### Why HOCON?
Superset of json, so compatible with appsettings.json format, with additional ergonomic features.

Typically you have appsettings for asp.net core and hocon for akka. However we can merge them!

## Tests

### Automation

#### Playwright
Install browser inside `devcontainer`:
```
pwsh ./bin/Debug/net7.0/playwright.ps1 install chromium
pwsh ./bin/Debug/net7.0/playwright.ps1 install-deps
```
