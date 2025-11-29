// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using NBAExtension.Data;

namespace NBAExtension;

internal sealed partial class NBAExtensionPage : DynamicListPage, IDisposable
{
    private static readonly HttpClient _httpClient = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly List<ListItem> _lastGames = [];
    private readonly Dictionary<ListItem, DateTime> _gameDates = [];
    private DateTime _lastFetch = DateTime.MinValue;
    private const string EspnApiUrl = "https://cdn.espn.com/core/nba/schedule?xhr=1";

    public NBAExtensionPage()
    {
        Icon = new IconInfo("https://a.espncdn.com/combiner/i?img=/i/teamlogos/leagues/500/nba.png&w=64&h=64&transparent=true"); 
        Title = "View Games";
        Name = "View Games";
        IsLoading = true;
    }

    public override void UpdateSearchText(string oldSearch, string newSearch) => RaiseItemsChanged();

    private static string GetTeamLogo(Team team)
    {
        return team.Logos?.FirstOrDefault()?.Href
                      ?? team.Logo
                      ?? "https://a.espncdn.com/i/teamlogos/nba/500/scoreboard/nba.png";
    }

    public override IListItem[] GetItems()
    {
        IsLoading = true;
        var delta = DateTime.UtcNow - _lastFetch;
        if (_lastGames.Count == 0 || delta.Minutes > 5)
        {
            var task = FetchGamesAsync();
            task.ConfigureAwait(false);
            task.Wait();
        }

        var searchText = SearchText ?? string.Empty;
        
        IListItem[] results;
        
        if (string.IsNullOrWhiteSpace(searchText))
        {
            // No search text - just sort by date
            results = _lastGames
                .OrderBy(item => _gameDates.TryGetValue(item, out var date) ? date : DateTime.MaxValue)
                .ToArray();
        }
        else
        {
            // With search text - filter and sort by date (all matches equal)
            results = _lastGames
                .Select(item => new
                {
                    Item = item,
                    MatchResult = StringMatcher.FuzzySearch(searchText, item.Title),
                    Date = _gameDates.TryGetValue(item, out var date) ? date : DateTime.MaxValue
                })
                .Where(x => x.MatchResult.Score > 0) // Only show matches
                .OrderBy(x => x.Date) // Sort by date first
                .Select(x => x.Item)
                .ToArray();
        }

        IsLoading = false;
        return results;
    }

    private async Task FetchGamesAsync()
    {
        _lastGames.Clear();
        _gameDates.Clear(); // Clear the dates dictionary
        _lastFetch = DateTime.UtcNow;

        try
        {
            var jsonString = await _httpClient.GetStringAsync(EspnApiUrl);

            // Write to temp file for debugging
            var tempFile = Path.Combine(Path.GetTempPath(), "nba_api_response.json");
            await File.WriteAllTextAsync(tempFile, jsonString);
            System.Diagnostics.Debug.WriteLine($"JSON written to: {tempFile}");

            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;

            // Try to navigate the JSON structure
            if (!root.TryGetProperty("content", out var content))
            {
                System.Diagnostics.Debug.WriteLine("No 'content' property found in response");
                return;
            }

            if (!content.TryGetProperty("schedule", out var schedule))
            {
                System.Diagnostics.Debug.WriteLine("No 'schedule' property found in content");
                return;
            }

            // Iterate through each date in the schedule
            foreach (var dateProperty in schedule.EnumerateObject())
            {
                var dateKey = dateProperty.Name;
                var dateValue = dateProperty.Value;

                if (!dateValue.TryGetProperty("games", out var games))
                {
                    continue;
                }

                foreach (var gameElement in games.EnumerateArray())
                {
                    try
                    {
                        var game = JsonSerializer.Deserialize<Game>(gameElement.GetRawText(), _jsonOptions);
                        if (game != null)
                        {
                            var listItem = CreateListItemFromGame(game);
                            if (listItem != null)
                            {
                                _lastGames.Add(listItem);
                                
                                // Store the game date for sorting
                                if (DateTime.TryParse(game.Date, out var gameDate))
                                {
                                    _gameDates[listItem] = gameDate;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing game: {ex.Message}");
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"Successfully loaded {_lastGames.Count} games");
        }
        catch (JsonException jsonEx)
        {
            System.Diagnostics.Debug.WriteLine($"JSON Parse Error: {jsonEx.Message}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching NBA schedule: {ex.Message}");
        }
    }

    private static ListItem? CreateListItemFromGame(Game game)
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
                time = "Final";
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

        var command = new NoOpCommand();
        var listItem = new ListItem(command)
        {
            Title = title,
            Icon = new IconInfo(GetTeamLogo(homeTeam.Team)),
            Tags = tags.ToArray(),
        };

        return listItem;
    }

    private static (string date, string time) FormatGameDateTime(string? dateString)
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

            var dayOfWeek = estTime.ToString("dddd");  // e.g., "Friday"

            var date = $"{dayOfWeek}, {estTime.ToString("MMM. d yyyy")}";
            var time = estTime.ToString("h:mm tt") + " EST";

            return (date, time);
        }

        return (dateString, "Time TBA");
    }

    public void Dispose()
    {
        // HttpClient is static and shared, so we don't dispose it here
    }
}