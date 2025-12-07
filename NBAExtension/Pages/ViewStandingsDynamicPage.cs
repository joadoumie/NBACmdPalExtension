// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using NBAExtension.Data.EspnStandingsResponse;
using NBAExtension.Helpers;
using System.Text.Json.Serialization;

namespace NBAExtension.Pages;

internal sealed partial class ViewStandingsDynamicPage : DynamicListPage, IDisposable
{
    private static readonly HttpClient _httpClient = new();
    private readonly Dictionary<string, List<(StandingsEntry Entry, int Seed)>> _standings = new();
    private DateTime _lastFetch = DateTime.MinValue;
    private const string EspnStandingsUrl = "https://site.web.api.espn.com/apis/v2/sports/basketball/nba/standings?region=us&lang=en&contentorigin=espn&type=1&level=2";

    public ViewStandingsDynamicPage()
    {
        Icon = new IconInfo("https://a.espncdn.com/combiner/i?img=/i/teamlogos/leagues/500/nba.png&w=64&h=64&transparent=true");
        Title = "View NBA Standings";
        Name = "View NBA Standings";

        var filters = new ConferenceFilters();
        filters.PropChanged += Filters_PropChanged;
        Filters = filters;
    }

    private void Filters_PropChanged(object sender, IPropChangedEventArgs args) => RaiseItemsChanged();

    public override void UpdateSearchText(string oldSearch, string newSearch) => RaiseItemsChanged();

    public override IListItem[] GetItems()
    {
        IsLoading = true;
        var delta = DateTime.UtcNow - _lastFetch;
        if (delta.Minutes > 5)
        {
            var task = FetchStandingsAsync();
            task.ConfigureAwait(false);
            task.Wait();
        }

        var searchText = SearchText ?? string.Empty;
        var items = new List<IListItem>();

        // Determine which conferences to show based on filter
        var conferencesToShow = new List<string>();
        
        if (string.IsNullOrEmpty(Filters.CurrentFilterId) || Filters.CurrentFilterId == "all")
        {
            conferencesToShow.Add("Eastern Conference");
            conferencesToShow.Add("Western Conference");
        }
        else if (Filters.CurrentFilterId == "eastern")
        {
            conferencesToShow.Add("Eastern Conference");
        }
        else if (Filters.CurrentFilterId == "western")
        {
            conferencesToShow.Add("Western Conference");
        }

        // Build items for each conference
        foreach (var conference in conferencesToShow)
        {
            if (!_standings.TryGetValue(conference, out var conferenceStandings))
            {
                continue;
            }

            // Add conference header
            var headerTag = new Tag(conference)
            {
                Background = ColorHelpers.FromArgb(255, 30, 30, 30),
                Foreground = ColorHelpers.FromArgb(255, 255, 255, 255)
            };

            var headerItem = new ListItem(new NoOpCommand())
            {
                Title = conference,
                Icon = new IconInfo("https://a.espncdn.com/combiner/i?img=/i/teamlogos/leagues/500/nba.png&w=64&h=64&transparent=true"),
                Tags = [headerTag]
            };

            items.Add(headerItem);

            // Sort by seed (1st to 15th) and add teams
            var sortedTeams = conferenceStandings
                .OrderBy(t => t.Seed)
                .ToList();

            foreach (var (entry, seed) in sortedTeams)
            {
                try
                {
                    // Apply search filter
                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        var teamName = entry.Team?.DisplayName ?? string.Empty;
                        if (FuzzyStringMatcher.ScoreFuzzy(searchText, teamName) <= 0)
                        {
                            continue;
                        }
                    }

                    var listItem = StandingsListItemFactory.CreateListItem(entry, conference);
                    if (listItem != null)
                    {
                        items.Add(listItem);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing standings entry: {ex.Message}");
                }
            }
        }

        IsLoading = false;
        
        if (items.Count == 0)
        {
            return [new ListItem(new NoOpCommand()) { Title = "No standings data found." }];
        }

        return items.ToArray();
    }

    private async Task FetchStandingsAsync()
    {
        _standings.Clear();
        _lastFetch = DateTime.UtcNow;

        try
        {
            var jsonString = await _httpClient.GetStringAsync(EspnStandingsUrl);

            System.Diagnostics.Debug.WriteLine($"Fetched standings data. Length: {jsonString.Length}");

            var response = JsonSerializer.Deserialize(jsonString, StandingsJsonContext.Default.StandingsResponse);

            if (response?.Children == null || response.Children.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("No conference data found in standings response");
                return;
            }

            // Process each conference
            foreach (var conference in response.Children)
            {
                if (!conference.IsConference || conference.Standings?.Entries == null)
                {
                    continue;
                }

                var conferenceName = conference.Name ?? "Unknown";
                System.Diagnostics.Debug.WriteLine($"Processing conference: {conferenceName} with {conference.Standings.Entries.Count} teams");

                var conferenceStandings = new List<(StandingsEntry Entry, int Seed)>();

                // Store each team with its seed
                foreach (var entry in conference.Standings.Entries)
                {
                    if (entry.Stats != null)
                    {
                        var seedStat = entry.Stats.FirstOrDefault(s => 
                            s.Name?.Equals("playoffSeed", StringComparison.OrdinalIgnoreCase) == true);
                        
                        var seed = seedStat != null ? (int)seedStat.Value : 99;
                        conferenceStandings.Add((entry, seed));
                    }
                }

                _standings[conferenceName] = conferenceStandings;
            }

            System.Diagnostics.Debug.WriteLine($"Successfully loaded standings for {_standings.Count} conferences");
        }
        catch (JsonException jsonEx)
        {
            System.Diagnostics.Debug.WriteLine($"JSON Parse Error: {jsonEx.Message}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching NBA standings: {ex.Message}");
        }
    }

    public void Dispose()
    {
        // HttpClient is static and shared, so we don't dispose it here
    }
}

#pragma warning disable SA1402 // File may only contain a single type
public partial class ConferenceFilters : Filters
#pragma warning restore SA1402 // File may only contain a single type
{
    public override IFilterItem[] GetFilters()
    {
        return
        [
            new Filter() { Id = "all", Name = "All Conferences" },
            new Filter() { Id = "eastern", Name = "Eastern Conference", Icon = new IconInfo("\uE82D") }, // East icon
            new Filter() { Id = "western", Name = "Western Conference", Icon = new IconInfo("\uE82E") }, // West icon
        ];
    }
}

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(StandingsResponse))]
[JsonSerializable(typeof(Conference))]
[JsonSerializable(typeof(ConferenceStandings))]
[JsonSerializable(typeof(StandingsEntry))]
[JsonSerializable(typeof(StandingsTeam))]
[JsonSerializable(typeof(TeamLogo))]
[JsonSerializable(typeof(TeamLink))]
[JsonSerializable(typeof(StandingStat))]
internal partial class StandingsJsonContext : JsonSerializerContext
{
}
