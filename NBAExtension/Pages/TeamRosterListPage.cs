// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using NBAExtension.Data;
using NBAExtension.Helpers;

namespace NBAExtension.Pages;

internal sealed partial class TeamRosterListPage : ListPage
{
    private static readonly HttpClient _httpClient = new();
    private readonly string _teamId;
    private readonly string _teamName;
    private readonly string _teamLogo;
    private List<RosterAthlete>? _roster;
    private bool _isLoaded;

    public TeamRosterListPage(string teamId, string teamName, string teamLogo)
    {
        _teamId = teamId ?? throw new ArgumentNullException(nameof(teamId));
        _teamName = teamName ?? "Team";
        _teamLogo = teamLogo ?? "https://a.espncdn.com/i/teamlogos/leagues/500/nba.png";

        Title = $"{_teamName} Roster";
        Icon = new IconInfo(_teamLogo);
    }

    public override IListItem[] GetItems()
    {
        if (!_isLoaded)
        {
            var task = LoadRosterAsync();
            task.ConfigureAwait(false);
            task.Wait();
            _isLoaded = true;
        }

        if (_roster == null || _roster.Count == 0)
        {
            return new[] { new ListItem(new NoOpCommand()) { Title = "No roster data available" } };
        }

        var items = new List<IListItem>();

        foreach (var athlete in _roster)
        {
            if (athlete == null)
            {
                continue;
            }

            var tags = new List<Tag>();

            // Add position tag
            if (!string.IsNullOrEmpty(athlete.Position?.Abbreviation))
            {
                tags.Add(new Tag(athlete.Position.Abbreviation));
            }

            // Add jersey number tag
            if (!string.IsNullOrEmpty(athlete.Jersey))
            {
                tags.Add(new Tag($"#{athlete.Jersey}"));
            }

            // Add experience tag
            if (athlete.Experience != null)
            {
                var yearsText = athlete.Experience.Years == 0 ? "Rookie" : 
                               athlete.Experience.Years == 1 ? "1 Year" : 
                               $"{athlete.Experience.Years} Years";
                tags.Add(new Tag(yearsText));
            }

            // Add injury status tag if injured
            if (athlete.Injuries != null && athlete.Injuries.Count > 0)
            {
                var injury = athlete.Injuries.FirstOrDefault();
                if (injury != null && !string.IsNullOrEmpty(injury.Status))
                {
                    var injuryTag = new Tag(injury.Status);
                    injuryTag.Background = ColorHelpers.FromArgb(255, 220, 53, 69); // Red background
                    injuryTag.Foreground = ColorHelpers.FromArgb(255, 255, 255, 255); // White text
                    tags.Add(injuryTag);
                }
            }

            // Get player card URL
            var playerUrl = athlete.Links?
                .FirstOrDefault(link => link.Rel?.Contains("playercard") == true)?.Href
                ?? $"https://www.espn.com/nba/player/_/id/{athlete.Id}";

            var command = new OpenUrlCommand(playerUrl) { Name = "View Player on ESPN" };

            var listItem = new ListItem(command)
            {
                Title = athlete.DisplayName ?? athlete.FullName ?? "Unknown Player",
                Icon = new IconInfo(athlete.Headshot?.Href ?? _teamLogo),
                Tags = tags.ToArray()
            };

            items.Add(listItem);
        }

        System.Diagnostics.Debug.WriteLine($"TeamRosterListPage: Returning {items.Count} roster items");
        return items.ToArray();
    }

    private async Task LoadRosterAsync()
    {
        try
        {
            var url = $"http://site.api.espn.com/apis/site/v2/sports/basketball/nba/teams/{_teamId}/roster";
            System.Diagnostics.Debug.WriteLine($"Fetching roster from: {url}");

            var jsonString = await _httpClient.GetStringAsync(url);
            var response = JsonSerializer.Deserialize(jsonString, RosterJsonContext.Default.EspnRosterResponse);

            if (response?.Athletes != null)
            {
                _roster = response.Athletes;
                System.Diagnostics.Debug.WriteLine($"Successfully loaded {_roster.Count} players");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No athletes found in roster response");
                _roster = new List<RosterAthlete>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading roster: {ex.Message}");
            _roster = new List<RosterAthlete>();
        }
    }
}

[JsonSerializable(typeof(EspnRosterResponse))]
[JsonSerializable(typeof(RosterAthlete))]
[JsonSerializable(typeof(Headshot))]
[JsonSerializable(typeof(RosterTeam))]
[JsonSerializable(typeof(Injury))]
[JsonSerializable(typeof(Experience))]
internal partial class RosterJsonContext : JsonSerializerContext
{
}