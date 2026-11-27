# NBA Command Palette Extension

<div align="center">

![NBA Logo](https://a.espncdn.com/combiner/i?img=/i/teamlogos/leagues/500/nba.png&w=128&h=128&transparent=true)

**All the things you may desire for the NBA right at your fingertips.**

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/download)
[![C# 13.0](https://img.shields.io/badge/C%23-13.0-blue.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)

</div>

---

## 📋 Overview

NBA Command Palette Extension brings real-time NBA game information directly into your Windows Command Palette. Stay updated on games, scores, schedules, standings, and player stats without leaving your workflow.

> **⚠️ Work in Progress**: This extension is actively being developed and is available on the [Microsoft Store](https://apps.microsoft.com/detail/joadoumie.NBACommandPaletteExtension). Choose which command runs on Enter for games, standings, and rosters in the extension settings. Next up: favorite-team preferences and auto-refresh for live games.

## ✨ Features

### 🏀 View Upcoming Games

Browse all NBA games for the upcoming week with rich details:

- **Live game information** with team records (e.g., Lakers (14-4) vs. Pelicans (4-14))
- **Real-time scores** for games in progress
- **Game status indicators** with quarter and time remaining
- **Team logos** and visual indicators
- **Smart search** with fuzzy matching to quickly find your favorite teams
- **Date and time** in EST with automatic timezone conversion
- **Enter** opens Team / Game Leaders right in the palette; **Ctrl+Enter** opens the game on ESPN
- **Configurable command order**: pick what Enter, Ctrl+Enter, and the context menu do in the extension settings

<div align="center">
  <img width="1380" height="830" alt="image" src="https://github.com/user-attachments/assets/908a8215-e070-4371-bbf4-c5cc12374e26" />
</div>

### 📊 Team & Game Leaders

View statistical leaders for any matchup:

- **View Team Leaders** for upcoming games - see season leaders for both teams (PPG, RPG, APG)
- **View Game Leaders** for live/completed games - see game-specific performance stats
- **Player headshots** and details (jersey number, position)

### 👥 Team Rosters

Browse complete team rosters with comprehensive player information:

- **Full roster listings** for any NBA team
- **Player details view** with bio, physical stats, and career information
- **Injury status tracking** with visual indicators (available/injured)
- **Player attributes**: position, jersey number, height, weight, age, experience
- **Quick links** to player stats, game logs, news, biography, and splits on ESPN
- **Player headshots** and visual presentation
- **Direct access** from any game listing to view either team's roster, or press Enter on a team in the standings

<img width="1889" height="968" alt="image" src="https://github.com/user-attachments/assets/d80a1605-04fe-4651-b8db-051e4ba0af03" />

### 📈 League Standings

Check the playoff picture at a glance:

- **Eastern and Western Conference** shown as separate sections
- **Seeded 1–15** with each team's win/loss record
- **Filter** by conference
- **Enter** opens the team's roster; **Ctrl+Enter** opens the team on ESPN
- **Reigning champion** tagged with a gold 🏆 badge
- **Team logos** on every row

> **New in 0.0.6.0:**
> - **Choose your commands.** Command Palette Settings → Extensions → NBA Command Palette Extension now lets you reorder the commands for games, standings, and roster players: click a command to pick it up, then drop it where you want it. The first runs on Enter, the second on Ctrl+Enter.
> - Games now open **Team / Game Leaders on Enter**; the ESPN game page moves to **Ctrl+Enter**. Prefer the old behavior? Move "View on ESPN" back to the top in settings.
>
> **0.0.5.0:** standings open the team roster on Enter, rosters load again after an ESPN endpoint change, and icons work offline.
>
> **0.0.3.0:** every list page (Games, Standings, Team Leaders, Rosters) groups its results into labeled sections with headers instead of one flat list.


## 🚀 Installation

> **Upgrading from 0.0.1.x?** Those builds shipped as an Inno `.exe` that
> registered a COM server but wasn't discoverable by PowerToys Command
> Palette. Uninstall them from *Settings → Apps → Installed apps*
> (search "NBA Command Palette Extension") before installing 0.0.2.0.

### Option 1: Microsoft Store

Install from the [Microsoft Store listing](https://apps.microsoft.com/detail/joadoumie.NBACommandPaletteExtension)
— this is the recommended path. The Store signs and delivers the MSIX, and
PowerToys picks it up automatically via the Command Palette AppExtension
contract.

### Option 2: winget

```powershell
winget install joadoumie.NBACommandPaletteExtension
```

winget routes to the Store listing under the hood, so this is equivalent to
Option 1.

### Option 3: From source (for contributors)

1. Clone the repository.
2. Open `NBAExtension.sln` in Visual Studio 2022 (17.12+) or later. Install
   these via the Visual Studio Installer first:
   - **Windows application development** workload (Windows App SDK + WinUI +
     the single-project MSIX deploy tooling)
   - **Desktop development with C++** workload — or at minimum the **MSVC
     v14x C++ x64/x86 build tools** (the CsWinRT AOT step shells out to MSVC)
   - **Windows 11 SDK 10.0.26100** (matches the project's target platform)
3. Set the active platform to **x64** (or **ARM64**) and **Release**, then
   **Deploy** the `NBAExtension` project. Deploying registers the MSIX on
   your machine so PowerToys can find it.
4. Open PowerToys Command Palette and search for "NBA".

## 🎮 Usage

1. Open **Windows Command Palette** (must be installed via PowerToys)
2. Type `NBA` or search for **"View Upcoming NBA Games"**
3. Browse upcoming games sorted by date
4. Use **fuzzy search** to filter by team name (e.g., type "lakers" or "LAL")
5. Press **Enter** on any game to see its **team or game leaders**, or **Ctrl+Enter** to open it on ESPN
6. Open the context menu (right-click) to **view either team's roster**
7. In **View NBA Standings**, press Enter on a team to open its roster, or Ctrl+Enter for the team's ESPN page

### Customizing command order

Open **Command Palette Settings → Extensions → NBA Command Palette Extension** to choose which command runs on **Enter** (primary), which runs on **Ctrl+Enter** (secondary), and the order of the rest of the context menu — separately for **View NBA Games**, **View NBA Standings**, and **Team Roster**. Click a command to pick it up, then click one of the **Move here** spots to drop it; changes save immediately. Use **Reset** on a list, or **Reset all to defaults**, to restore the original order.

### Search Tips

- Type team names: `lakers`, `warriors`, `celtics`
- Using abbreviations: `LAL`, `GSW`, `BOS` is **NOT CURRENTLY SUPPORTED**... and idk if it ever will be? maybe?
- Search by matchup: `lakers vs warriors`

### Key Components

- **Source Generation**: Uses JSON source generation for AOT compatibility and performance
- **Caching**: 5-minute cache for API responses to reduce load
- **Fuzzy Search**: Smart search algorithm for quick team filtering
- **Dynamic Updates**: Real-time score updates for live games
- **Context-Aware Commands**: Different leader views based on game status
- **Rich Details View**: Enhanced player and team information with metadata

## 🗺️ Roadmap

- [x] View upcoming games for the week
- [x] Display team records
- [x] Live score updates
- [x] Fuzzy search functionality
- [x] View team leaders (season stats)
- [x] View game leaders (in-progress/completed games)
- [x] Team roster viewing with detailed player info
- [x] Player injury status tracking
- [x] Team standings (Eastern/Western, sectioned)
- [x] Sectioned list pages with headers
- [x] Add releases to GitHub
- [ ] User preferences for favorite teams
- [ ] Player stats integration
- [x] WinGet distribution (0.0.1.x via Inno; 0.0.2.0+ via Microsoft Store)
- [ ] Auto-refresh for live games

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **Microsoft** friends for the Command Palette Extensions SDK. So much fun to build with.
- NBA fans everywhere 🏀

## 📞 Support

If you encounter any issues or have suggestions:

- 🐛 [Report a bug](https://github.com/joadoumie/NBACmdPalExtension/issues/new?template=bug_report.yml)
- 💡 [Request a feature](https://github.com/joadoumie/NBACmdPalExtension/issues/new?template=feature_request.yml)
- ⭐ Star this repository if you find it useful!

---

<div align="center">
Made with ❤️ by NBA fans for NBA fans
</div>
