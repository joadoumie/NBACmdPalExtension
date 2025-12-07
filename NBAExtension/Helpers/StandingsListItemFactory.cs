// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using NBAExtension.Data.EspnStandingsResponse;
using NBAExtension.Pages;

namespace NBAExtension.Helpers;

/// <summary>
/// Factory class for creating list items from NBA standings data.
/// </summary>
internal static class StandingsListItemFactory
{
    /// <summary>
    /// Creates a ListItem from a StandingsEntry object.
    /// </summary>
    /// <param name="entry">The standings entry.</param>
    /// <param name="conference">The conference name.</param>
    /// <returns>A ListItem configured for the standings entry, or null if the data is invalid.</returns>
    public static ListItem? CreateListItem(StandingsEntry entry, string conference)
    {
        if (entry.Team == null || entry.Stats == null || entry.Stats.Count == 0)
        {
            return null;
        }

        var team = entry.Team;
        var teamLogoUrl = GetTeamLogo(team);

        // Get key stats
        var wins = GetStatValue(entry.Stats, "wins");
        var losses = GetStatValue(entry.Stats, "losses");
        var winPct = GetStatDisplayValue(entry.Stats, "winPercent");
        var gamesBehind = GetStatDisplayValue(entry.Stats, "gamesBehind");
        var streak = GetStatDisplayValue(entry.Stats, "streak");
        var playoffSeed = GetStatValue(entry.Stats, "playoffSeed");
        var overallRecord = GetStatSummary(entry.Stats, "overall");
        var homeRecord = GetStatSummary(entry.Stats, "Home");
        var awayRecord = GetStatSummary(entry.Stats, "Road");
        var confRecord = GetStatSummary(entry.Stats, "vs. Conf.");

        // Build title with just team name
        var title = team.DisplayName ?? "Unknown Team";

        // Build subtitle with ranking and records
        var rank = GetOrdinalSuffix(playoffSeed);
        var subtitleParts = new List<string> { rank };
        
        if (!string.IsNullOrEmpty(homeRecord))
        {
            subtitleParts.Add($"Home: {homeRecord}");
        }
        if (!string.IsNullOrEmpty(awayRecord))
        {
            subtitleParts.Add($"Away: {awayRecord}");
        }
        if (!string.IsNullOrEmpty(confRecord))
        {
            subtitleParts.Add($"Conf: {confRecord}");
        }

        var subtitle = string.Join(" • ", subtitleParts);

        // Build tags
        var tags = new List<Tag>();

        // Record tag
        if (!string.IsNullOrEmpty(overallRecord))
        {
            tags.Add(new Tag(overallRecord));
        }

        // Win % tag
        if (!string.IsNullOrEmpty(winPct))
        {
            tags.Add(new Tag(winPct));
        }

        // Games Behind tag (if not the leader)
        if (!string.IsNullOrEmpty(gamesBehind) && gamesBehind != "-")
        {
            tags.Add(new Tag($"GB: {gamesBehind}"));
        }

        // Streak tag with color coding
        if (!string.IsNullOrEmpty(streak))
        {
            var streakTag = new Tag(streak);
            
            // Color code the streak (green for wins, red for losses)
            if (streak.StartsWith("W"))
            {
                streakTag.Background = ColorHelpers.FromArgb(255, 0, 128, 0); // Green
                streakTag.Foreground = ColorHelpers.FromArgb(255, 255, 255, 255); // White
            }
            else if (streak.StartsWith("L"))
            {
                streakTag.Background = ColorHelpers.FromArgb(255, 178, 34, 34); // Dark Red
                streakTag.Foreground = ColorHelpers.FromArgb(255, 255, 255, 255); // White
            }
            
            tags.Add(streakTag);
        }

        // Conference tag
        tags.Add(new Tag(conference));

        // Create team link command
        var teamLink = team.Links?.FirstOrDefault(l => 
            l.Rel?.Contains("clubhouse") == true && 
            l.Rel?.Contains("desktop") == true);

        var viewTeamCommand = new OpenUrlCommand(teamLink?.Href ?? $"https://www.espn.com/nba/team/_/name/{team.Abbreviation?.ToLowerInvariant()}")
        {
            Name = $"View {team.DisplayName} on ESPN",
            Result = CommandResult.Dismiss()
        };

        // Create more commands list
        var moreCommands = new List<IContextItem>();

        // Add roster command if team ID is available
        if (team.Id != null && !string.IsNullOrEmpty(team.DisplayName))
        {
            var rosterPage = new TeamRosterListPage(
                team.Id,
                team.DisplayName,
                teamLogoUrl)
            {
                Name = $"View {team.ShortDisplayName ?? team.DisplayName} Roster",
                Icon = new IconInfo(teamLogoUrl)
            };
            moreCommands.Add(new CommandContextItem(rosterPage));
        }

        var listItem = new ListItem(viewTeamCommand)
        {
            Title = title,
            Subtitle = subtitle,
            Icon = new IconInfo(teamLogoUrl),
            Tags = tags.ToArray(),
            MoreCommands = moreCommands.ToArray()
        };

        return listItem;
    }

    /// <summary>
    /// Converts a number to its ordinal representation (1st, 2nd, 3rd, etc.).
    /// </summary>
    /// <param name="number">The number as a string.</param>
    /// <returns>The ordinal representation (e.g., "1st", "2nd", "3rd").</returns>
    private static string GetOrdinalSuffix(string number)
    {
        if (string.IsNullOrEmpty(number) || !int.TryParse(number, out var n))
        {
            return number;
        }

        if (n <= 0)
        {
            return number;
        }

        // Handle special cases for 11th, 12th, 13th
        if (n % 100 >= 11 && n % 100 <= 13)
        {
            return $"{n}th";
        }

        // Handle general cases
        return (n % 10) switch
        {
            1 => $"{n}st",
            2 => $"{n}nd",
            3 => $"{n}rd",
            _ => $"{n}th"
        };
    }

    /// <summary>
    /// Gets the logo URL for a team.
    /// </summary>
    /// <param name="team">The team.</param>
    /// <returns>The logo URL.</returns>
    private static string GetTeamLogo(StandingsTeam team)
    {
        // Try to get the default logo first
        var defaultLogo = team.Logos?.FirstOrDefault(l => l.Rel?.Contains("default") == true);
        if (defaultLogo?.Href != null)
        {
            return defaultLogo.Href;
        }

        // Fall back to first available logo
        return team.Logos?.FirstOrDefault()?.Href
               ?? "https://a.espncdn.com/i/teamlogos/nba/500/scoreboard/nba.png";
    }

    /// <summary>
    /// Gets the display value of a stat by name.
    /// </summary>
    /// <param name="stats">The stats list.</param>
    /// <param name="statName">The stat name.</param>
    /// <returns>The display value, or empty string if not found.</returns>
    private static string GetStatDisplayValue(List<StandingStat> stats, string statName)
    {
        var stat = stats.FirstOrDefault(s => s.Name?.Equals(statName, System.StringComparison.OrdinalIgnoreCase) == true);
        return stat?.DisplayValue ?? string.Empty;
    }

    /// <summary>
    /// Gets the numeric value of a stat by name.
    /// </summary>
    /// <param name="stats">The stats list.</param>
    /// <param name="statName">The stat name.</param>
    /// <returns>The value, or empty string if not found.</returns>
    private static string GetStatValue(List<StandingStat> stats, string statName)
    {
        var stat = stats.FirstOrDefault(s => s.Name?.Equals(statName, System.StringComparison.OrdinalIgnoreCase) == true);
        return stat != null ? stat.Value.ToString("F0") : string.Empty;
    }

    /// <summary>
    /// Gets the summary value of a stat by name.
    /// </summary>
    /// <param name="stats">The stats list.</param>
    /// <param name="statName">The stat name.</param>
    /// <returns>The summary value, or empty string if not found.</returns>
    private static string GetStatSummary(List<StandingStat> stats, string statName)
    {
        var stat = stats.FirstOrDefault(s => s.Name?.Equals(statName, System.StringComparison.OrdinalIgnoreCase) == true);
        return stat?.Summary ?? string.Empty;
    }
}
