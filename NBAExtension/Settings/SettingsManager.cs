// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace NBAExtension.Settings;

/// <summary>
/// Owns the extension's persisted settings: the command order for each list page.
/// </summary>
internal sealed class SettingsManager
{
    private readonly string _filePath;
    private readonly object _lock = new();

    public SettingsManager()
        : this(Path.Combine(Utilities.BaseSettingsPath("NBAExtension"), "settings.json"))
    {
    }

    internal SettingsManager(string filePath)
    {
        _filePath = filePath;

        // Definition order is the default order. Games open leaders in-palette on Enter (ESPN on
        // Ctrl+Enter); games without leader data fall back to ESPN on Enter.
        Games = new ActionOrderGroup<GameCommandAction>(
            "games",
            "View NBA Games",
            "Commands for each game in the schedule.",
            (GameCommandAction.TeamLeaders, "leaders", "View Team / Game Leaders"),
            (GameCommandAction.ViewOnEspn, "espn", "View on ESPN"),
            (GameCommandAction.HomeTeamRoster, "home_roster", "View Home Team Roster"),
            (GameCommandAction.AwayTeamRoster, "away_roster", "View Away Team Roster"));

        Standings = new ActionOrderGroup<StandingsCommandAction>(
            "standings",
            "View NBA Standings",
            "Commands for each team in the standings.",
            (StandingsCommandAction.TeamRoster, "roster", "View Team Roster"),
            (StandingsCommandAction.ViewOnEspn, "espn", "View Team on ESPN"));

        Roster = new ActionOrderGroup<RosterCommandAction>(
            "roster",
            "Team Roster",
            "Commands for each player on a team roster.",
            (RosterCommandAction.PlayerProfile, "profile", "View Player on ESPN"),
            (RosterCommandAction.Stats, "stats", "View Stats"),
            (RosterCommandAction.AdvancedStats, "advancedstats", "View Advanced Stats"),
            (RosterCommandAction.GameLog, "gamelog", "View Game Log"),
            (RosterCommandAction.News, "news", "View News"),
            (RosterCommandAction.Biography, "bio", "View Biography"),
            (RosterCommandAction.Splits, "splits", "View Splits"));

        Groups = [Games, Standings, Roster];

        Load();
    }

    /// <summary>Raised after any setting changes and has been saved.</summary>
    public event EventHandler? SettingsChanged;

    public ActionOrderGroup<GameCommandAction> Games { get; }

    public ActionOrderGroup<StandingsCommandAction> Standings { get; }

    public ActionOrderGroup<RosterCommandAction> Roster { get; }

    public IReadOnlyList<IActionOrderGroup> Groups { get; }

    /// <summary>
    /// Runs a batch of edits, then saves and notifies once if anything changed.
    /// </summary>
    /// <param name="edit">Returns true if it changed any setting.</param>
    /// <returns>True if anything changed.</returns>
    public bool Update(Func<SettingsManager, bool> edit)
    {
        bool changed;
        lock (_lock)
        {
            changed = edit(this);
            if (changed)
            {
                Save();
            }
        }

        if (changed)
        {
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        return changed;
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return;
            }

            var data = JsonSerializer.Deserialize(File.ReadAllText(_filePath), SettingsJsonContext.Default.SettingsData);
            if (data?.CommandOrder == null)
            {
                return;
            }

            foreach (var group in Groups)
            {
                if (data.CommandOrder.TryGetValue(group.Id, out var keys))
                {
                    group.Load(keys);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading NBAExtension settings: {ex.Message}");
        }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

            var data = new SettingsData
            {
                CommandOrder = Groups.ToDictionary(g => g.Id, g => g.CurrentKeys.ToList()),
            };

            File.WriteAllText(_filePath, JsonSerializer.Serialize(data, SettingsJsonContext.Default.SettingsData));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving NBAExtension settings: {ex.Message}");
        }
    }
}

#pragma warning disable SA1402 // File may only contain a single type
internal sealed class SettingsData
{
    [JsonPropertyName("commandOrder")]
    public Dictionary<string, List<string>>? CommandOrder { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SettingsData))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext
{
}
#pragma warning restore SA1402 // File may only contain a single type
