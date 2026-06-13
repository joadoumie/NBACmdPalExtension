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
using NBAExtension.Data.EspnScheduleResponse;
using NBAExtension.Helpers;
using System.Text.Json.Serialization;

namespace NBAExtension;

internal sealed partial class ViewGamesDynamicPage : DynamicListPage, IDisposable
{
    private static readonly HttpClient _httpClient = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly List<(ListItem Item, DateTime Date, string DateLabel)> _lastGames = [];
    private DateTime _lastFetch = DateTime.MinValue;
    private const string EspnApiUrl = "https://cdn.espn.com/core/nba/schedule?xhr=1&render=false&device=desktop&userab=18";

    // TODO: Use date parameter to fetch specific dates if needed and allow user to set date preferences?
    private static string BuildEspnUrl(DateTime date)
    {
        // ESPN expects YYYYMMDD, zero-padded
        var dateParam = date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        // Mirrors your TS version:
        // baseUrl + params: xhr=1, render=false, device=desktop, userab=18
        return $"https://cdn.espn.com/core/nba/schedule" +
               $"?dates={dateParam}" +
               $"&xhr=1" +
               $"&render=false" +
               $"&device=desktop" +
               $"&userab=18";
    }

    public ViewGamesDynamicPage()
    {
        Icon = new IconInfo("https://a.espncdn.com/combiner/i?img=/i/teamlogos/leagues/500/nba.png&w=64&h=64&transparent=true"); 
        Title = "View Games";
        Name = "View Games";
    }

    public override void UpdateSearchText(string oldSearch, string newSearch) => RaiseItemsChanged();

    public override IListItem[] GetItems()
    {
        IsLoading = true;
        var delta = DateTime.UtcNow - _lastFetch;
        if (delta.Minutes > 5)
        {
            var task = FetchGamesAsync();
            task.ConfigureAwait(false);
            task.Wait();
        }

        var searchText = SearchText ?? string.Empty;

        IEnumerable<(ListItem Item, DateTime Date, string DateLabel)> filtered;

        if (string.IsNullOrWhiteSpace(searchText))
        {
            filtered = _lastGames.OrderBy(g => g.Date);
        }
        else
        {
            filtered = _lastGames
                .Where(g => FuzzyStringMatcher.ScoreFuzzy(searchText, g.Item.Title) > 0)
                .OrderBy(g => g.Date);
        }

        // Group games by their date label
        var grouped = filtered
            .GroupBy(g => g.DateLabel)
            .OrderBy(g => g.Min(x => x.Date))
            .ToList();

        var items = new List<IListItem>();
        foreach (var group in grouped)
        {
            // Section prepends a Separator header for the date, then that day's games.
            items.AddRange(new Section(group.Key, group.Select(g => (IListItem)g.Item).ToArray()));
        }

        IsLoading = false;

        if (items.Count == 0 && string.IsNullOrEmpty(searchText))
        {
            return [new ListItem(new NoOpCommand()) { Title = "No games found." }];
        }

        return items.ToArray();
    }

    private async Task FetchGamesAsync()
    {
        _lastGames.Clear();
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
                        var game = JsonSerializer.Deserialize(gameElement.GetRawText(), GameJsonContext.Default.Game);
                        if (game != null)
                        {
                            var listItem = GameListItemFactory.CreateListItem(game);
                            if (listItem != null)
                            {
                                DateTime gameDate = DateTime.MaxValue;
                                string dateLabel = dateKey;
                                if (DateTime.TryParse(game.Date, out var parsedDate))
                                {
                                    gameDate = parsedDate;
                                    var (label, _) = GameListItemFactory.FormatGameDateTime(game.Date);
                                    dateLabel = label;
                                }

                                _lastGames.Add((listItem, gameDate, dateLabel));
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
 
    public void Dispose()
    {
        // HttpClient is static and shared, so we don't dispose it here
    }
}

[JsonSerializable(typeof(Game))]
[JsonSerializable(typeof(Competition))]
[JsonSerializable(typeof(Competitor))]
[JsonSerializable(typeof(Team))]
[JsonSerializable(typeof(TeamRecord))]
[JsonSerializable(typeof(GameStatus))]
[JsonSerializable(typeof(StatusType))]
[JsonSerializable(typeof(LineScore))]
[JsonSerializable(typeof(Logo))]
internal partial class GameJsonContext : JsonSerializerContext
{
}