// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using NBAExtension.Pages;

namespace NBAExtension;

public partial class NBAExtensionCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    // Return more commands like GH links etc. 
    private static IContextItem[] GetAboutContextItems()
    {
        return [
            new CommandContextItem(new OpenUrlCommand("https://github.com/joadoumie/NBACmdPalExtension/issues/new?template=feature_request.yml") { Name = "Request New Feature", Result = CommandResult.Dismiss() }) { Icon = new IconInfo("\uD83C\uDFC0") },
            new CommandContextItem(new OpenUrlCommand("https://github.com/joadoumie/NBACmdPalExtension/issues/new?template=bug_report.yml") { Name = "Report a Bug", Result = CommandResult.Dismiss() }) { Icon = new IconInfo("\uD83D\uDC1B") },
            new CommandContextItem(new OpenUrlCommand("https://github.com/joadoumie/NBACmdPalExtension") { Name = "View Source Code", Result = CommandResult.Dismiss() }) { Icon = new IconInfo("\u2328\uFE0F") },
        ];
    }

    public NBAExtensionCommandsProvider()
    {
        DisplayName = "NBA Command Palette Extension";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        _commands = [
            // Offseason takeover: the Knicks won the 2026 title and there are no upcoming
            // games, so this entry opens the celebration page instead of an empty schedule.
            // To restore the live schedule when the NBA returns, swap `new KnicksChampionsPage()`
            // back to `new ViewGamesDynamicPage()` (the schedule page is kept intact below).
            new CommandItem(new KnicksChampionsPage()) { Title = "🏆 New York Knicks — 2026 NBA Champions", Subtitle = "NBA games & schedule return next season — tap to celebrate", Icon = new IconInfo("https://a.espncdn.com/i/teamlogos/nba/500/ny.png"), MoreCommands = GetAboutContextItems() },
            new CommandItem(new ViewStandingsDynamicPage()) { Title = "View NBA Standings", Icon = new IconInfo("https://a.espncdn.com/combiner/i?img=/i/teamlogos/leagues/500/nba.png&w=64&h=64&transparent=true"), MoreCommands = GetAboutContextItems() },
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

}
