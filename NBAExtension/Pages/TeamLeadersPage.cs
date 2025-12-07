// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using NBAExtension.Data.EspnScheduleResponse;
using NBAExtension.Helpers;

namespace NBAExtension.Pages;

internal sealed partial class TeamLeadersPage : ListPage
{
    private readonly Game _game;

    public TeamLeadersPage(Game game)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        
        var competition = game.Competitions?.FirstOrDefault();
        var homeTeam = competition?.Competitors?.FirstOrDefault(c => c.HomeAway?.Equals("home", StringComparison.OrdinalIgnoreCase) == true);
        var awayTeam = competition?.Competitors?.FirstOrDefault(c => c.HomeAway?.Equals("away", StringComparison.OrdinalIgnoreCase) == true);

        Title = $"Team Leaders: {awayTeam?.Team?.ShortDisplayName ?? "Away"} vs. {homeTeam?.Team?.ShortDisplayName ?? "Home"}";
        Icon = new IconInfo("https://a.espncdn.com/combiner/i?img=/i/teamlogos/leagues/500/nba.png&w=64&h=64&transparent=true");
    }

    public override IListItem[] GetItems()
    {
        var items = new List<IListItem>();
        var competition = _game.Competitions?.FirstOrDefault();

        if (competition?.Competitors == null)
        {
            System.Diagnostics.Debug.WriteLine("TeamLeadersPage: No competition or competitors found");
            return new[] { new ListItem(new NoOpCommand()) { Title = "No team leaders data available" } };
        }

        System.Diagnostics.Debug.WriteLine($"TeamLeadersPage: Processing {competition.Competitors.Count} competitors");

        // Process both teams
        foreach (var competitor in competition.Competitors)
        {
            if (competitor.Team == null)
            {
                System.Diagnostics.Debug.WriteLine("TeamLeadersPage: Competitor has no team");
                continue;
            }

            System.Diagnostics.Debug.WriteLine($"TeamLeadersPage: Processing team {competitor.Team.DisplayName}");
            System.Diagnostics.Debug.WriteLine($"TeamLeadersPage: Leaders count = {competitor.Leaders?.Count ?? 0}");

            // Add team header regardless of whether we have leaders
            var teamHeader = new ListItem(new NoOpCommand())
            {
                Title = $"{competitor.Team.DisplayName} Leaders",
                Icon = new IconInfo(GameListItemFactory.GetTeamLogo(competitor.Team)),
                Tags = new[] { new Tag("Team") }
            };
            items.Add(teamHeader);

            if (competitor.Leaders == null || competitor.Leaders.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"TeamLeadersPage: No leaders data for {competitor.Team.DisplayName}");
                items.Add(new ListItem(new NoOpCommand()) 
                { 
                    Title = "No leader data available for this team" 
                });
                continue;
            }

            // Debug: Log all leader categories
            foreach (var cat in competitor.Leaders)
            {
                System.Diagnostics.Debug.WriteLine($"TeamLeadersPage: Found category {cat.Abbreviation} - {cat.DisplayName}");
            }

            // Add ALL leader categories (not just PTS, REB, AST)
            foreach (var category in competitor.Leaders)
            {
                if (category.Leaders == null || category.Leaders.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"TeamLeadersPage: Category {category.Abbreviation} has no leaders");
                    continue;
                }

                var leader = category.Leaders.FirstOrDefault();
                if (leader == null)
                {
                    System.Diagnostics.Debug.WriteLine($"TeamLeadersPage: Category {category.Abbreviation} has null leader");
                    continue;
                }

                System.Diagnostics.Debug.WriteLine($"TeamLeadersPage: Processing leader for {category.Abbreviation}");

                var tags = new List<Tag>
                {
                    new Tag($"{category.Abbreviation}: {leader.DisplayValue ?? "N/A"}")
                };

                // Only add optional tags if they exist
                if (leader.Athlete != null)
                {
                    if (!string.IsNullOrEmpty(leader.Athlete.Jersey))
                    {
                        tags.Add(new Tag($"#{leader.Athlete.Jersey}"));
                    }

                    if (!string.IsNullOrEmpty(leader.Athlete.Position?.Abbreviation))
                    {
                        tags.Add(new Tag(leader.Athlete.Position.Abbreviation));
                    }
                }

                var athleteName = leader.Athlete?.DisplayName ?? "Unknown Player";
                var athleteId = leader.Athlete?.Id ?? "0";
                var headshot = leader.Athlete?.Headshot;

                var athleteUrl = leader.Athlete?.Links?
                    .FirstOrDefault(link => link.Rel?.Contains("playercard") == true)?.Href 
                    ?? $"https://www.espn.com/nba/player/_/id/{athleteId}";

                var command = new OpenUrlCommand(athleteUrl) { Name = "View Player on ESPN" };
                
                var listItem = new ListItem(command)
                {
                    Title = $"{category.DisplayName ?? category.Abbreviation}: {athleteName}",
                    Icon = new IconInfo(headshot ?? GameListItemFactory.GetTeamLogo(competitor.Team)),
                    Tags = tags.ToArray()
                };

                items.Add(listItem);
            }

            // Add spacer between teams
            if (competitor != competition.Competitors.Last())
            {
                items.Add(new ListItem(new NoOpCommand()) { Title = " " });
            }
        }

        System.Diagnostics.Debug.WriteLine($"TeamLeadersPage: Returning {items.Count} items");

        return items.Count > 0 
            ? items.ToArray() 
            : new[] { new ListItem(new NoOpCommand()) { Title = "No team leaders data available" } };
    }
}
