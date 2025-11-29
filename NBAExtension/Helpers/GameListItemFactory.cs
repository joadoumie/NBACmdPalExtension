// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using NBAExtension.Data;

namespace NBAExtension.Helpers;

/// <summary>
/// Factory class for creating list items from NBA game data.
/// </summary>
internal static class GameListItemFactory
{
    /// <summary>
    /// Creates a ListItem from a Game object.
    /// </summary>
    /// <param name="game">The game data.</param>
    /// <returns>A ListItem configured for the game, or null if the game data is invalid.</returns>
    public static ListItem? CreateListItem(Game game)
    {
        if (game.Competitions == null || game.Competitions.Count == 0)
        {
            return null;
        }

        var competition = game.Competitions[0];
        if (competition.Competitors == null || competition.Competitors.Count < 2)
        {
            return null;
        }

        // Find home and away teams
        var homeTeam = competition.Competitors.FirstOrDefault(c => c.HomeAway?.Equals("home", StringComparison.OrdinalIgnoreCase) == true);
        var awayTeam = competition.Competitors.FirstOrDefault(c => c.HomeAway?.Equals("away", StringComparison.OrdinalIgnoreCase) == true);

        if (homeTeam?.Team == null || awayTeam?.Team == null)
        {
            return null;
        }

        var homeIconUrl = GetTeamLogo(homeTeam.Team);

        // Format the game date and time
        var (gameDate, gameTime) = FormatGameDateTime(game.Date);

        var title = $"{homeTeam.Team.ShortDisplayName} vs. {awayTeam.Team.ShortDisplayName}";

        var tags = new List<Tag> { new Tag(gameDate), new Tag(gameTime) };

        if (competition.Status?.Period != 0)
        {
            var homeScore = homeTeam.Score ?? "0";
            var awayScore = awayTeam.Score ?? "0";
            var time = $"Q{competition.Status?.Period}: {competition.Status?.DisplayClock}";
            var awayIconUrl = GetTeamLogo(awayTeam.Team);

            var homeTag = new Tag(homeScore);
            var awayTag = new Tag(awayScore);
            homeTag.Icon = new IconInfo(homeIconUrl);
            awayTag.Icon = new IconInfo(awayIconUrl);

            if (competition.Status?.Type?.Completed == true)
            {
                if (int.TryParse(homeScore, NumberStyles.Integer, CultureInfo.InvariantCulture, out var homeScoreValue) &&
                    int.TryParse(awayScore, NumberStyles.Integer, CultureInfo.InvariantCulture, out var awayScoreValue))
                {
                    if (homeScoreValue > awayScoreValue)
                    {
                        homeTag.Background = ColorHelpers.FromArgb(255, 0, 128, 0);
                        homeTag.Foreground = ColorHelpers.FromArgb(255, 255, 255, 255);
                    }
                    else
                    {
                        awayTag.Background = ColorHelpers.FromArgb(255, 0, 128, 0);
                        awayTag.Foreground = ColorHelpers.FromArgb(255, 255, 255, 255);
                    }
                }
            }
            tags.Add(homeTag);
            tags.Add(awayTag);
            tags.Add(new Tag(time));
        }

        var command = new OpenUrlCommand($"https://www.espn.com/nba/game/_/gameId/{game.Id}") { Name = "View on ESPN" };
        var listItem = new ListItem(command)
        {
            Title = title,
            Icon = new IconInfo(GetTeamLogo(homeTeam.Team)),
            Tags = tags.ToArray(),
        };

        return listItem;
    }

    /// <summary>
    /// Gets the logo URL for a team.
    /// </summary>
    /// <param name="team">The team.</param>
    /// <returns>The logo URL.</returns>
    public static string GetTeamLogo(Team team)
    {
        return team.Logos?.FirstOrDefault()?.Href
                      ?? team.Logo
                      ?? "https://a.espncdn.com/i/teamlogos/nba/500/scoreboard/nba.png";
    }

    /// <summary>
    /// Formats the game date and time into separate strings.
    /// </summary>
    /// <param name="dateString">The date string from the API.</param>
    /// <returns>A tuple containing the formatted date and time.</returns>
    public static (string date, string time) FormatGameDateTime(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString))
        {
            return ("Date TBA", "Time TBA");
        }

        if (DateTime.TryParse(dateString, out var gameDateTime))
        {
            // Convert to EST (Eastern Standard Time)
            var estZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var estTime = TimeZoneInfo.ConvertTimeFromUtc(gameDateTime.ToUniversalTime(), estZone);

            var dayOfWeek = estTime.ToString("dddd", CultureInfo.InvariantCulture);  // e.g., "Friday"

            var date = $"{dayOfWeek}, {estTime.ToString("MMM. d yyyy", CultureInfo.InvariantCulture)}";
            var time = estTime.ToString("h:mm tt", CultureInfo.InvariantCulture) + " EST";

            return (date, time);
        }

        return (dateString, "Time TBA");
    }
}