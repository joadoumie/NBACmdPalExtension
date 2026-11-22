# AGENTS.md

Instructions for AI coding agents working in **NBA Command Palette Extension** (PowerToys Command Palette extension in .NET 9 / C# 13, distributed as an MSIX package for Windows `x64` and `ARM64`).

## Build & Verification

There is currently no automated test project in the solution. Verify code changes with a clean Debug build and check for new trim, AOT, or analyzer errors.

- **Debug build (fast inner loop; runs trim/AOT/single-file analyzers without ILLink trimming):**
  ```powershell
  dotnet build NBAExtension/NBAExtension.csproj -c Debug -p:Platform=x64
  ```
- **Release CLI build (matches CI):**
  ```powershell
  msbuild NBAExtension/NBAExtension.csproj /restore /p:Configuration=Release /p:Platform=x64 /p:RuntimeIdentifier=win-x64 /p:SelfContained=true /p:PublishReadyToRun=true /p:UapAppxPackageBuildMode=SideloadOnly /p:AppxBundle=Never
  ```
  - Command-line Release builds **must** pass `RuntimeIdentifier`, `SelfContained=true`, and `PublishReadyToRun=true` explicitly. `NBAExtension.csproj` references `PublishProfiles\win-$(Platform).pubxml` for Visual Studio, which is gitignored and not auto-loaded by CLI builds (omitting these flags fails with `NETSDK1102`).
- **Deploy / smoke-test locally:** Build & Deploy `NBAExtension` in Visual Studio (`x64` or `ARM64`) so MSIX registers the `com.microsoft.commandpalette` AppExtension.

## Repository Layout

- `NBAExtension/Pages/` — Command Palette list/dynamic pages (games, standings, rosters, team/game leaders)
- `NBAExtension/Helpers/` — List item factories (`GameListItemFactory`, `StandingsListItemFactory`) and bundled icons (`NbaIcons`)
- `NBAExtension/Data/` — ESPN API DTOs (deserialized via System.Text.Json source generation)
- `NBAExtension/Assets/` — Bundled MSIX visual assets and offline league logo (`NBALogo.png`)
- `NBAExtension/Package.appxmanifest` — Single source of truth for the 4-part package `<Identity Version>`
- `Directory.Packages.props` — Central NuGet package versions
- `.github/workflows/release-msix.yml` — CI workflow that builds the unsigned x64+ARM64 `.msixbundle` for Microsoft Store
- `winget/` and `archive/inno-installer/` — **Retired / historical.** Ignore for new work and releases.
- `release-staging/` — Gitignored local folder for downloaded `.msixbundle` artifacts and release notes. Never commit.

## Critical Invariants & Gotchas

1. **Command Palette Sections API (`GetItems()`, NOT `GetSections()`):**
   - Sections are returned from `override IListItem[] GetItems()` as a flat array using the Toolkit `Section(title, IListItem[])` helper (which prepends a `Separator` header):
     ```csharp
     items.AddRange(new Section("Eastern Conference", sectionItems.ToArray()));
     ```
   - There is **no** `GetSections()`, `IListSection`, or `ListSection` in `Microsoft.CommandPalette.Extensions`.
   - Keep `Microsoft.CommandPalette.Extensions` at **`>= 0.9`** in `Directory.Packages.props`; older versions (e.g. `0.6`) lack `Section`.

2. **Do NOT remove `ToolGood.Words.Pinyin` / `ToolGood.Words`:**
   - No `.cs` file references ToolGood directly, so it looks unused.
   - It is an **unlisted runtime dependency** of `Microsoft.CommandPalette.Extensions.Toolkit`'s `FuzzyStringMatcher.ScoreFuzzy(...)` used across list pages. Removing it breaks fuzzy search at runtime.

3. **Native AOT & Trimming Compatibility:**
   - `IsAotCompatible` and `CsWinRTAotOptimizerEnabled` are enabled; Release builds enable `PublishTrimmed=true`.
   - Always use `System.Text.Json` source generation (`JsonSerializerContext`) for JSON deserialization—do not add reflection-based serialization.

4. **MSIX / Microsoft Store Distribution Only:**
   - PowerToys Command Palette discovers extensions exclusively through `AppExtensionCatalog` (`uap3:AppExtension Name="com.microsoft.commandpalette"` in `Package.appxmanifest`).
   - Unpackaged `.exe` / COM-only installers (like the archived Inno Setup script) cannot register with Command Palette.

5. **Reigning Champion Badge Is Intentional:**
   - `IsChampionTeam` in `NBAExtension/Pages/ViewStandingsDynamicPage.cs` and the `isChampion` gold badge in `NBAExtension/Helpers/StandingsListItemFactory.cs` are deliberate product features (kept after retiring the 2026 celebration takeover).
   - Do **not** delete them as leftover code; update the matched team and championship year when a new NBA champion is crowned.

6. **Keep Source Files UTF-8:**
   - Always save `.cs` files as UTF-8 (subtitles use ` • ` and badges use emoji like `🏆`). Avoid Windows-1252 re-encoding.

7. **Git Workflow:**
   - Direct pushes to `main` are blocked by branch policy. Always commit changes on a feature/chore/release branch and open a pull request.

## Skills (On-Demand Runbooks)

- **Cutting a Microsoft Store release / local unsigned bundle testing:** Read `.agents/skills/release-msix/SKILL.md`.
