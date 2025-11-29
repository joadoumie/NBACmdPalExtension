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
using NBAExtension.Helpers;
using System.Text.Json.Serialization;

namespace NBAExtension;

internal sealed partial class ViewGamesDynamicPage : DynamicListPage, IDisposable
{
    private static readonly HttpClient _httpClient = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly List<ListItem> _lastGames = [];
    private readonly Dictionary<ListItem, DateTime> _gameDates = [];
    private DateTime _lastFetch = DateTime.MinValue;
    private const string EspnApiUrl = "https://cdn.espn.com/core/nba/schedule?xhr=1";

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
        if (results.Length == 0 && string.IsNullOrEmpty(searchText))
        {
            return [new ListItem(new NoOpCommand()) { Title = "No games found." },];
        }
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
                        var game = JsonSerializer.Deserialize(gameElement.GetRawText(), GameJsonContext.Default.Game);
                        if (game != null)
                        {
                            var listItem = GameListItemFactory.CreateListItem(game);
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
 
    public void Dispose()
    {
        // HttpClient is static and shared, so we don't dispose it here
    }
}

[JsonSerializable(typeof(Game))]
internal partial class GameJsonContext : JsonSerializerContext
{
}