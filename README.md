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

NBA Command Palette Extension brings real-time NBA game information directly into your Windows Command Palette. Stay updated on games, scores, schedules, and player stats without leaving your workflow.

> **⚠️ Work in Progress**: This extension is actively being developed. Next milestone: publish to WinGet!

## ✨ Features

### 🏀 View Upcoming Games

Browse all NBA games for the upcoming week with rich details:

- **Live game information** with team records (e.g., Lakers (14-4) vs. Pelicans (4-14))
- **Real-time scores** for games in progress
- **Game status indicators** with quarter and time remaining
- **Team logos** and visual indicators
- **Smart search** with fuzzy matching to quickly find your favorite teams
- **Date and time** in EST with automatic timezone conversion
- **Direct links** to ESPN for full game details

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
- **Direct access** from any game listing to view either team's roster

## 🚀 Installation

### Option 1: From Source (Current)

1. **Clone the repository**
2. **Open in Visual Studio**: Open the solution file in Visual Studio 2022 or newer.
3. **Deploy the extension**
   - Open the project in Visual Studio 2022+
   - Set the build configuration to **Debug** or **Release**
   - Build and run (F5)

### Option 2: WinGet (Coming Soon)

## 🎮 Usage

1. Open **Windows Command Palette** (must be installed via PowerToys)
2. Type `NBA` or search for **"View Upcoming NBA Games"**
3. Browse upcoming games sorted by date
4. Use **fuzzy search** to filter by team name (e.g., type "lakers" or "LAL")
5. Click any game to **view full details on ESPN**
6. Right-click or use context menu to **view team rosters** or **statistical leaders**

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
- [ ] User preferences for favorite teams
- [ ] Player stats integration
- [ ] Team standings
- [ ] WinGet distribution
- [ ] Add releases to GitHub (no clue how to do this yet)
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

- 🐛 [Report a bug](https://github.com/joadoumie/NBACmdPalExtension/issues)
- 💡 [Request a feature](https://github.com/joadoumie/NBACmdPalExtension/issues)
- ⭐ Star this repository if you find it useful!

---

<div align="center">
Made with ❤️ by NBA fans for NBA fans
</div>
