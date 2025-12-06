// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace NBAExtension;

public partial class NBAExtensionCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    // Return more commands like GH links etc. 
    private static IContextItem[] GetAboutContextItems()
    {
        return [
            new CommandContextItem(new OpenUrlCommand("https://github.com/joadoumie/NBACmdPalExtension/issues/new") { Name = "Request New Feature", Result = CommandResult.Dismiss() }) { Icon = new IconInfo("\uD83C\uDFC0") },
            new CommandContextItem(new OpenUrlCommand("https://github.com/joadoumie/NBACmdPalExtension/issues/new") { Name = "Report a Bug", Result = CommandResult.Dismiss() }) { Icon = new IconInfo("\uD83D\uDC1B") },
            new CommandContextItem(new OpenUrlCommand("https://github.com/joadoumie/NBACmdPalExtension") { Name = "View Source Code", Result = CommandResult.Dismiss() }) { Icon = new IconInfo("\u2328\uFE0F") },
        ];
    }

    public NBAExtensionCommandsProvider()
    {
        DisplayName = "View Upcoming NBA Games";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        _commands = [
            new CommandItem(new ViewGamesDynamicPage()) { Title = DisplayName, Icon = new IconInfo("https://a.espncdn.com/combiner/i?img=/i/teamlogos/leagues/500/nba.png&w=64&h=64&transparent=true"), MoreCommands = GetAboutContextItems() },
            new CommandItem(new SampleGalleryListPage()) { Title = "Sample Gallery List Page" },
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

}
