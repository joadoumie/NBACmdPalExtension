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
using NBAExtension.Settings;

namespace NBAExtension.Pages;

internal sealed partial class TeamRosterListPage : DynamicListPage
{
    private static readonly HttpClient _httpClient = new();
    private readonly string _teamId;
    private readonly string _teamName;
    private readonly string _teamLogo;
    private readonly SettingsManager _settings;
    private List<RosterAthlete>? _roster;
    private bool _isLoaded;

    public TeamRosterListPage(string teamId, string teamName, string teamLogo, SettingsManager settings)
    {
        _settings = settings;
        _teamId = teamId ?? throw new ArgumentNullException(nameof(teamId));
        _teamName = teamName ?? "Team";
        _teamLogo = teamLogo ?? "https://a.espncdn.com/i/teamlogos/leagues/500/nba.png";

        Title = $"{_teamName} Roster";
        Icon = new IconInfo(_teamLogo);
        ShowDetails = true; // Enable details view
        
        var filters = new InjuryStatusFilters();
        filters.PropChanged += Filters_PropChanged;
        Filters = filters;
    }

    private void Filters_PropChanged(object sender, IPropChangedEventArgs args) => RaiseItemsChanged();

    public override void UpdateSearchText(string oldSearch, string newSearch) => RaiseItemsChanged();

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
            return [new ListItem(new NoOpCommand()) { Title = "No roster data available" }];
        }

        var filteredRoster = _roster.AsEnumerable();

        // Apply injury status filter
        if (!string.IsNullOrEmpty(Filters.CurrentFilterId))
        {
            switch (Filters.CurrentFilterId)
            {
                case "active":
                    filteredRoster = filteredRoster.Where(athlete =>
                        athlete.Injuries == null || athlete.Injuries.Count == 0);
                    break;
                case "other":
                    filteredRoster = filteredRoster.Where(athlete =>
                        athlete.Injuries != null && athlete.Injuries.Count > 0);
                    break;
                case "all":
                default:
                    // No filtering
                    break;
            }
        }

        // Apply fuzzy search across name, position, and jersey (consistent with the other pages).
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filteredRoster = filteredRoster.Where(athlete =>
                FuzzyStringMatcher.ScoreFuzzy(SearchText, athlete.DisplayName ?? string.Empty) > 0 ||
                FuzzyStringMatcher.ScoreFuzzy(SearchText, athlete.FullName ?? string.Empty) > 0 ||
                FuzzyStringMatcher.ScoreFuzzy(SearchText, athlete.Position?.DisplayName ?? string.Empty) > 0 ||
                FuzzyStringMatcher.ScoreFuzzy(SearchText, athlete.Jersey ?? string.Empty) > 0);
        }

        // Separate into active and injured players
        var activeItems = new List<IListItem>();
        var injuredItems = new List<IListItem>();

        foreach (var athlete in filteredRoster)
        {
            if (athlete == null)
            {
                continue;
            }

            var tags = new List<Tag>();
            var descriptionParts = new List<string>();

            if (!string.IsNullOrEmpty(athlete.Position?.DisplayName))
            {
                descriptionParts.Add(athlete.Position.DisplayName);
            }

            if (!string.IsNullOrEmpty(athlete.Jersey))
            {
                descriptionParts.Add($"#{athlete.Jersey}");
            }

            var isInjured = athlete.Injuries != null && athlete.Injuries.Count > 0;

            if (isInjured)
            {
                var injury = athlete.Injuries!.FirstOrDefault();
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

            // First command runs on Enter; the rest go to the context menu (the first of those gets Ctrl+Enter).
            var ordered = _settings.Roster.Arrange(CreatePlayerCommands(athlete));

            var listItem = new ListItem(ordered[0].Command)
            {
                Title = athlete.DisplayName ?? athlete.FullName ?? "Unknown Player",
                Subtitle = descriptionParts.Count > 0 ? string.Join(" • ", descriptionParts) : string.Empty,
                Icon = new IconInfo(athlete.Headshot?.Href ?? _teamLogo),
                Tags = tags.Count > 0 ? tags.ToArray() : Array.Empty<Tag>(),
                MoreCommands = ordered.Skip(1).Select(c => (IContextItem)new CommandContextItem(c.Command) { Icon = c.Icon }).ToArray(),
                Details = CreatePlayerDetails(athlete)
            };

            if (isInjured)
            {
                injuredItems.Add(listItem);
            }
            else
            {
                activeItems.Add(listItem);
            }
        }

        var items = new List<IListItem>();

        if (activeItems.Count > 0)
        {
            // Section prepends a Separator "Active" header, then the active players.
            items.AddRange(new Section("Active", activeItems.ToArray()));
        }

        if (injuredItems.Count > 0)
        {
            // Section prepends a Separator "Injury Report" header, then the injured players.
            items.AddRange(new Section("Injury Report", injuredItems.ToArray()));
        }

        if (items.Count == 0)
        {
            return [new ListItem(new NoOpCommand()) { Title = "No players match the current filter" }];
        }

        System.Diagnostics.Debug.WriteLine($"TeamRosterListPage: Returning {items.Count} items");
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

    private static Dictionary<RosterCommandAction, (ICommand Command, IconInfo? Icon)> CreatePlayerCommands(RosterAthlete athlete)
    {
        var playerUrl = athlete.Links?
            .FirstOrDefault(link => link.Rel?.Contains("playercard") == true)?.Href
            ?? $"https://www.espn.com/nba/player/_/id/{athlete.Id}";

        var commands = new Dictionary<RosterCommandAction, (ICommand Command, IconInfo? Icon)>
        {
            [RosterCommandAction.PlayerProfile] = (new OpenUrlCommand(playerUrl) { Name = "View Player on ESPN" }, null),
        };

        // Define the links we want to show and their icons
        var linkConfigs = new[]
        {
            new { Action = RosterCommandAction.Stats, Rel = "stats", Name = "View Stats", Icon = "\uE9D9" }, // Chart icon
            new { Action = RosterCommandAction.AdvancedStats, Rel = "advancedstats", Name = "View Advanced Stats", Icon = "\uE9D2" }, // AreaChart icon
            new { Action = RosterCommandAction.GameLog, Rel = "gamelog", Name = "View Game Log", Icon = "\uE81C" }, // Calendar icon
            new { Action = RosterCommandAction.News, Rel = "news", Name = "View News", Icon = "\uE789" }, // News icon
            new { Action = RosterCommandAction.Biography, Rel = "bio", Name = "View Biography", Icon = "\uE77B" }, // Contact icon
            new { Action = RosterCommandAction.Splits, Rel = "splits", Name = "View Splits", Icon = "\uE8BC" }, // Split view icon
        };

        foreach (var config in linkConfigs)
        {
            var href = athlete.Links?.FirstOrDefault(l =>
                l.Rel?.Any(r => r.Equals(config.Rel, StringComparison.OrdinalIgnoreCase)) == true)?.Href;

            if (string.IsNullOrEmpty(href) && config.Action == RosterCommandAction.AdvancedStats && !string.IsNullOrEmpty(athlete.Id))
            {
                // Fallback: Construct advanced stats URL manually if not provided by API
                var nameSlug = CreateUrlSlug(athlete.DisplayName ?? athlete.FullName ?? "player");
                href = $"https://www.espn.com/nba/player/advancedstats/_/id/{athlete.Id}/{nameSlug}";
            }

            if (!string.IsNullOrEmpty(href))
            {
                commands[config.Action] = (
                    new OpenUrlCommand(href) { Name = config.Name, Result = CommandResult.Dismiss() },
                    new IconInfo(config.Icon));
            }
        }

        return commands;
    }

    private static string CreateUrlSlug(string name)
    {
        // Convert name to URL-friendly slug (e.g., "Nick Smith Jr" -> "nick-smith-jr")
        return name.ToLowerInvariant()
                   .Replace(" ", "-")
                   .Replace(".", "")
                   .Replace("'", "")
                   .Trim('-');
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
            // site.web.api.espn.com serves the same payload as site.api.espn.com, which now
            // answers 403 to requests without a browser User-Agent (like this HttpClient).
            var url = $"https://site.web.api.espn.com/apis/site/v2/sports/basketball/nba/teams/{_teamId}/roster";
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

#pragma warning disable SA1402 // File may only contain a single type
public partial class InjuryStatusFilters : Filters
#pragma warning restore SA1402 // File may only contain a single type
{
    public override IFilterItem[] GetFilters()
    {
        return
        [
            new Filter() { Id = "all", Name = "All Players" },
            new Filter() { Id = "active", Name = "Active", Icon = new IconInfo("\uE73E") }, // Checkmark icon
            new Filter() { Id = "other", Name = "Injury Report", Icon = new IconInfo("\uE7BA") }, // Warning icon
        ];
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