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
using NBAExtension.Data.EspnRosterResponse;
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
        ShowDetails = true; // Enable details view
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
            var descriptionParts = new List<string>();

            // Add position to description
            if (!string.IsNullOrEmpty(athlete.Position?.Abbreviation))
            {
                descriptionParts.Add(athlete.Position.Abbreviation);
            }

            // Add jersey number to description
            if (!string.IsNullOrEmpty(athlete.Jersey))
            {
                descriptionParts.Add($"#{athlete.Jersey}");
            }

            // Add injury status tag if injured
            if (athlete.Injuries != null && athlete.Injuries.Count > 0)
            {
                var injury = athlete.Injuries.FirstOrDefault();
                if (injury != null && !string.IsNullOrEmpty(injury.Status))
                {
                    var injuryTag = new Tag(injury.Status)
                    {
                        Background = ColorHelpers.FromArgb(255, 220, 53, 69),
                        Foreground = ColorHelpers.FromArgb(255, 255, 255, 255)
                    };
                    tags.Add(injuryTag);
                }
            }

            // Get player card URL
            var playerUrl = athlete.Links?
                .FirstOrDefault(link => link.Rel?.Contains("playercard") == true)?.Href
                ?? $"https://www.espn.com/nba/player/_/id/{athlete.Id}";

            var command = new OpenUrlCommand(playerUrl) { Name = "View Player on ESPN" };

            // Create context items for more commands
            var moreCommands = CreatePlayerContextItems(athlete);

            var listItem = new ListItem(command)
            {
                Title = athlete.DisplayName ?? athlete.FullName ?? "Unknown Player",
                Subtitle = descriptionParts.Count > 0 ? string.Join(" • ", descriptionParts) : string.Empty,
                Icon = new IconInfo(athlete.Headshot?.Href ?? _teamLogo),
                Tags = tags.Count > 0 ? tags.ToArray() : Array.Empty<Tag>(),
                MoreCommands = moreCommands.Length > 0 ? moreCommands : null,
                Details = CreatePlayerDetails(athlete)
            };

            items.Add(listItem);
        }

        System.Diagnostics.Debug.WriteLine($"TeamRosterListPage: Returning {items.Count} roster items");
        return items.ToArray();
    }

    private Details CreatePlayerDetails(RosterAthlete athlete)
    {
        var metadata = new List<DetailsElement>();

        // Separator before Basic Bio Info 
        metadata.Add(new DetailsElement
        {
            Key = "Bio",
            Data = new DetailsSeparator()
        });

        // Basic Info Section
        metadata.Add(new DetailsElement
        {
            Key = "Position",
            Data = new DetailsLink 
            { 
                Text = athlete.Position?.DisplayName ?? athlete.Position?.Abbreviation ?? "N/A" 
            }
        });

        if (!string.IsNullOrEmpty(athlete.Jersey))
        {
            metadata.Add(new DetailsElement
            {
                Key = "Jersey Number",
                Data = new DetailsLink { Text = $"#{athlete.Jersey}" }
            });
        }

        // Physical Attributes
        if (!string.IsNullOrEmpty(athlete.DisplayHeight))
        {
            metadata.Add(new DetailsElement
            {
                Key = "Height",
                Data = new DetailsLink { Text = athlete.DisplayHeight }
            });
        }

        if (!string.IsNullOrEmpty(athlete.DisplayWeight))
        {
            metadata.Add(new DetailsElement
            {
                Key = "Weight",
                Data = new DetailsLink { Text = athlete.DisplayWeight }
            });
        }

        // Experience & Age
        if (athlete.Experience != null)
        {
            var experienceText = athlete.Experience.Years == 0 ? "Rookie" :
                                athlete.Experience.Years == 1 ? "1 Year" :
                                $"{athlete.Experience.Years} Years";
            metadata.Add(new DetailsElement
            {
                Key = "Experience",
                Data = new DetailsLink { Text = experienceText }
            });
        }

        if (athlete.Age > 0)
        {
            metadata.Add(new DetailsElement
            {
                Key = "Age",
                Data = new DetailsLink { Text = $"{athlete.Age} years old" }
            });
        }

        // Injury Status
        if (athlete.Injuries != null && athlete.Injuries.Count > 0)
        {
            var injury = athlete.Injuries.FirstOrDefault();
            if (injury != null && !string.IsNullOrEmpty(injury.Status))
            {
                metadata.Add(new DetailsElement
                {
                    Key = "Injury Status",
                    Data = new DetailsTags
                    {
                        Tags = new[]
                        {
                            new Tag(injury.Status)
                            {
                                Background = ColorHelpers.FromArgb(255, 220, 53, 69),
                                Foreground = ColorHelpers.FromArgb(255, 255, 255, 255),
                                Icon = new IconInfo("\uE7BA") // Warning icon
                            }
                        }
                    }
                });
            }
        }
        else
        {
            metadata.Add(new DetailsElement
            {
                Key = "Injury Status",
                Data = new DetailsTags
                {
                    Tags = new[]
                    {
                       new Tag("Available")
                       {
                           // Nice green background with white text
                           Background = ColorHelpers.FromArgb(255, 40, 167, 69),
                           Foreground = ColorHelpers.FromArgb(255, 255, 255, 255),
                           Icon = new IconInfo("\uE73E") // Checkmark icon

                       }
                   }
                }
            });
        }

        var details = new Details
        {
            Title = athlete.DisplayName ?? athlete.FullName ?? "Unknown Player",
            Body = BuildPlayerBio(athlete),
            Metadata = metadata.ToArray()
        };

        return details;
    }

    private static IContextItem[] CreatePlayerContextItems(RosterAthlete athlete)
    {
        if (athlete.Links == null || athlete.Links.Count == 0)
        {
            return Array.Empty<IContextItem>();
        }

        var contextItems = new List<IContextItem>();

        // Define the links we want to show and their icons
        var linkConfigs = new[]
        {
            new { Rel = "stats", Name = "View Stats", Icon = "\uE9D9" }, // Chart icon
            new { Rel = "gamelog", Name = "View Game Log", Icon = "\uE81C" }, // Calendar icon
            new { Rel = "news", Name = "View News", Icon = "\uE789" }, // News icon
            new { Rel = "bio", Name = "View Biography", Icon = "\uE77B" }, // Contact icon
            new { Rel = "splits", Name = "View Splits", Icon = "\uE8BC" }, // Split view icon
        };

        foreach (var config in linkConfigs)
        {
            var link = athlete.Links.FirstOrDefault(l => 
                l.Rel?.Any(r => r.Equals(config.Rel, StringComparison.OrdinalIgnoreCase)) == true);

            if (link != null && !string.IsNullOrEmpty(link.Href))
            {
                contextItems.Add(new CommandContextItem(new OpenUrlCommand(link.Href)
                {
                    Name = config.Name,
                    Result = CommandResult.Dismiss()
                })
                {
                    Icon = new IconInfo(config.Icon)
                });
            }
        }

        return contextItems.ToArray();
    }

    private string BuildPlayerBio(RosterAthlete athlete)
    {
        var bio = new List<string>();

        // Add player headshot as larger image in markdown
        var headshotUrl = athlete.Headshot?.Href ?? _teamLogo;
        bio.Add($"<img src=\"{headshotUrl}\" alt=\"{athlete.DisplayName}\" width=\"250\" />");
        bio.Add(""); // Empty line for spacing

        return string.Join("\n", bio);
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